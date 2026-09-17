using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using ForceOutfitChange.Core;

namespace ForceOutfitChange.Patches
{
    /// <summary>
    /// Harmony patch that intercepts vanilla apparel optimization jobs and replaces them
    /// with our custom dropping versions when they're player-forced outfit changes.
    /// </summary>
    [HarmonyPatch(typeof(JobGiver_OptimizeApparel), "TryGiveJob")]
    public static class JobGiver_OptimizeApparel_Patch
    {
        /// <summary>
        /// Postfix that replaces vanilla apparel jobs with our custom dropping versions
        /// when the change was forced by the player (through our button).
        /// </summary>
        public static void Postfix(ref Job __result, Pawn pawn)
        {
            // Only modify jobs when we have a valid result and the pawn has a force outfit change marker
            if (__result == null || !ShouldUseCustomJobs(pawn))
            {
                return;
            }
            
            Job customJob = null;
            
            // Handle different job types
            if (__result.def == JobDefOf.RemoveApparel)
            {
                // Replace removal jobs with our custom version that drops clothes
                customJob = JobMaker.MakeJob(ForceOutfitChangeDefOf.ForceRemoveApparel, __result.targetA);
                customJob.playerForced = __result.playerForced;
                customJob.haulDroppedApparel = false;
            }
            else if (__result.def == JobDefOf.Wear)
            {
                // For wear jobs, use our custom version that drops conflicting clothes at the target location
                customJob = JobMaker.MakeJob(ForceOutfitChangeDefOf.ForceWear, __result.targetA);
                if (__result.targetB.IsValid)
                {
                    customJob.targetB = __result.targetB; // For apparel sources
                }
                customJob.playerForced = __result.playerForced;
            }
            
            if (customJob != null)
            {
                // Replace the vanilla job with our custom one
                __result = customJob;
                
                if (Prefs.DevMode)
                {
                    Log.Message($"[ForceOutfitChange] Replaced vanilla job with custom dropping job: {customJob.def.reportString} for {pawn.LabelShort}");
                }
            }
        }
        
        /// <summary>
        /// Determines if we should use custom jobs for this pawn.
        /// We use a temporary marker to know when outfit changes were triggered by our button.
        /// </summary>
        private static bool ShouldUseCustomJobs(Pawn pawn)
        {
            // Check if this pawn has been marked for force outfit change
            return ForceOutfitChangeMarker.ShouldUseCustomJobs(pawn);
        }
    }
}
