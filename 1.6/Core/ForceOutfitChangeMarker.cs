using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace ForceOutfitChange.Core
{
    /// <summary>
    /// Static helper that tracks which pawns should use our custom dropping jobs.
    /// </summary>
    public static class ForceOutfitChangeMarker
    {
        private static readonly HashSet<int> markedPawns = new HashSet<int>();
        
        /// <summary>
        /// Marks a pawn as needing custom dropping jobs for their outfit optimization.
        /// </summary>
        public static void MarkPawnForCustomJobs(Pawn pawn)
        {
            if (pawn?.thingIDNumber != null)
            {
                markedPawns.Add(pawn.thingIDNumber);
                
                if (Prefs.DevMode)
                {
                    Log.Message($"[ForceOutfitChange] Marked {pawn.LabelShort} for custom dropping jobs");
                }
            }
        }
        
        /// <summary>
        /// Checks if a pawn should use custom dropping jobs and removes the marker after use.
        /// </summary>
        public static bool ShouldUseCustomJobs(Pawn pawn)
        {
            if (pawn?.thingIDNumber == null)
            {
                return false;
            }
            
            bool shouldUse = markedPawns.Contains(pawn.thingIDNumber);
            if (shouldUse)
            {
                // Remove the marker after checking so it only applies to this job
                markedPawns.Remove(pawn.thingIDNumber);
                
                if (Prefs.DevMode)
                {
                    Log.Message($"[ForceOutfitChange] Using custom job for {pawn.LabelShort}");
                }
            }
            
            return shouldUse;
        }
        
        /// <summary>
        /// Clears all markers. Called when game loads/starts to prevent stale data.
        /// </summary>
        public static void ClearAllMarkers()
        {
            markedPawns.Clear();
            if (Prefs.DevMode)
            {
                Log.Message("[ForceOutfitChange] Cleared all pawn markers");
            }
        }
    }
}
