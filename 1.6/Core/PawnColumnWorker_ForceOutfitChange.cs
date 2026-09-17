using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using Verse.Sound;
using ForceOutfitChange.Core;

namespace ForceOutfitChange.Core
{
    /// <summary>
    /// Custom pawn column worker that adds a force outfit change button next to each pawn's outfit assignment.
    /// Supports drag-to-apply functionality for applying the command to multiple pawns at once.
    /// </summary>
    public class PawnColumnWorker_ForceOutfitChange : PawnColumnWorker
    {
        private const int ButtonSize = 24;
        private const int ButtonPadding = 2;
        
        private static List<Pawn> pawnsToForceChange = new List<Pawn>();
        
        /// <summary>
        /// Draws the cell content - a button that forces outfit change when clicked.
        /// </summary>
        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            if (pawn.outfits == null || pawn.IsQuestLodger())
            {
                return;
            }
            
            // Center the button in the cell
            Rect buttonRect = new Rect(
                rect.x + (rect.width - ButtonSize) / 2f,
                rect.y + (rect.height - ButtonSize) / 2f,
                ButtonSize,
                ButtonSize
            );
            
            // Determine button texture and tooltip
            Texture2D buttonTex = TexButton.Reload;
            string tooltip = "ForceOutfitChange_ButtonTooltip".Translate();
            
            // Handle mouse interaction
            if (Mouse.IsOver(buttonRect))
            {
                Widgets.DrawHighlight(buttonRect);
                TooltipHandler.TipRegion(buttonRect, tooltip);
                
                // Support drag functionality if column is paintable
                if (def.paintable && Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    if (!pawnsToForceChange.Contains(pawn))
                    {
                        pawnsToForceChange.Add(pawn);
                    }
                }
            }
            
            // Draw the button
            if (Widgets.ButtonImage(buttonRect, buttonTex))
            {
                ForceOutfitChange(pawn);
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
            
            // Handle drag release
            if (def.paintable && Event.current.type == EventType.MouseUp && Event.current.button == 0)
            {
                if (pawnsToForceChange.Count > 0)
                {
                    foreach (Pawn p in pawnsToForceChange)
                    {
                        // Don't double-process the same pawn if they were also clicked directly
                        if (p != pawn)
                        {
                            ForceOutfitChange(p);
                        }
                    }
                    pawnsToForceChange.Clear();
                    SoundDefOf.Click.PlayOneShotOnCamera();
                }
            }
        }
        
        /// <summary>
        /// Forces the pawn to immediately change their outfit to match their assigned policy.
        /// Uses vanilla apparel selection logic but marks the pawn so our Harmony patch
        /// will replace vanilla jobs with our custom dropping versions.
        /// </summary>
        private void ForceOutfitChange(Pawn pawn)
        {
            if (pawn?.mindState == null || pawn.outfits?.CurrentApparelPolicy == null)
            {
                return;
            }
            
            // Wake up sleeping pawns
            if (pawn.CurJob?.def == JobDefOf.LayDown && !pawn.Awake())
            {
                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
            
            // If the pawn has a current job that isn't critical, interrupt it
            if (pawn.CurJob != null && !IsEssentialJob(pawn.CurJob))
            {
                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
            
            // Mark this pawn so our Harmony patch will use custom dropping jobs
            ForceOutfitChangeMarker.MarkPawnForCustomJobs(pawn);
            
            // Directly generate and queue apparel optimization jobs immediately
            GenerateAndQueueApparelJobs(pawn);
            
            if (Prefs.DevMode)
            {
                Messages.Message($"ForceOutfitChange_Forced".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.NeutralEvent, false);
            }
        }
        
        /// <summary>
        /// Determines if a job is essential and shouldn't be interrupted.
        /// </summary>
        private bool IsEssentialJob(Job job)
        {
            // Don't interrupt firefighting, doctoring, or other critical jobs
            // return job.def == JobDefOf.BeatFire ||
            //        job.def == JobDefOf.TendPatient ||
            //        job.def == JobDefOf.Rescue ||
            //        job.def == JobDefOf.ExtinguishSelf ||
            //        job.def == JobDefOf.FleeAndCower;
			return job.def == JobDefOf.Rescue || job.def == JobDefOf.FleeAndCower;
        }
        
        /// <summary>
        /// Directly generates and queues apparel optimization jobs for immediate execution.
        /// </summary>
        private void GenerateAndQueueApparelJobs(Pawn pawn)
        {
            // Wait a tick for the pawn to be in the right state after job interruption
            Delay.AfterNTicks(1, delegate
            {
                try
                {
                    // Use reflection to access the protected TryGiveJob method
                    var jobGiver = new JobGiver_OptimizeApparel();
                    var method = typeof(JobGiver_OptimizeApparel).GetMethod("TryGiveJob", 
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    
                    if (method != null)
                    {
                        // Keep generating and queuing jobs until no more are needed
                        int maxJobs = 10; // Safety limit to prevent infinite loops
                        int jobCount = 0;
                        
                        while (jobCount < maxJobs)
                        {
                            try
                            {
                                Job job = (Job)method.Invoke(jobGiver, new object[] { pawn });
                                
                                if (job == null)
                                {
                                    break; // No more apparel jobs needed
                                }
                                
                                // Our Harmony patch will intercept and modify this job
                                job.playerForced = true;
                                
                                // Queue the job with highest priority - force it to execute immediately
                                pawn.jobs.TryTakeOrderedJob(job, JobTag.MiscWork);
                                
                                jobCount++;
                                
                                if (Prefs.DevMode)
                                {
                                    Log.Message($"[ForceOutfitChange] Queued job {job.def.reportString} for {pawn.LabelShort}");
                                }
                            }
                            catch (System.Exception ex)
                            {
                                Log.Error($"[ForceOutfitChange] Error generating apparel job: {ex.Message}");
                                break;
                            }
                        }
                        
                        if (jobCount == 0)
                        {
                            // Try alternative approach - trigger normal optimization but mark for interception
                            pawn.mindState.Notify_OutfitChanged();
                            
                            if (Prefs.DevMode)
                            {
                                Log.Message($"[ForceOutfitChange] No direct jobs found, triggering normal optimization for {pawn.LabelShort}");
                            }
                        }
                    }
                    else
                    {
                        Log.Error("[ForceOutfitChange] Could not access JobGiver_OptimizeApparel.TryGiveJob method via reflection");
                        // Fallback to the normal method
                        pawn.mindState.Notify_OutfitChanged();
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[ForceOutfitChange] Exception in delayed job generation: {ex.Message}");
                    // Final fallback
                    pawn.mindState.Notify_OutfitChanged();
                }
            });
        }
        

        
        /// <summary>
        /// Gets the minimum width for this column.
        /// </summary>
        public override int GetMinWidth(PawnTable table)
        {
            return ButtonSize + (ButtonPadding * 2);
        }
        
        /// <summary>
        /// Gets the optimal width for this column.
        /// </summary>
        public override int GetOptimalWidth(PawnTable table)
        {
            return GetMinWidth(table);
        }
        
        /// <summary>
        /// Gets the maximum width for this column.
        /// </summary>
        public override int GetMaxWidth(PawnTable table)
        {
            return GetMinWidth(table);
        }
        
        /// <summary>
        /// Draws the column header.
        /// </summary>
        public override void DoHeader(Rect rect, PawnTable table)
        {
            // Draw a small icon in the header
            Texture2D headerTex = TexButton.Reload;
            float iconSize = Mathf.Min(rect.width - 4, rect.height - 4);
            Rect iconRect = new Rect(
                rect.x + (rect.width - iconSize) / 2f,
                rect.y + (rect.height - iconSize) / 2f,
                iconSize,
                iconSize
            );
            
            GUI.DrawTexture(iconRect, headerTex);
            
            if (Mouse.IsOver(rect))
            {
                TooltipHandler.TipRegion(rect, "ForceOutfitChange_HeaderTooltip".Translate());
            }
        }
    }
}
