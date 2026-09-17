using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using ForceOutfitChange.Core;

namespace ForceOutfitChange.Patches
{
    /// <summary>
    /// Harmony patch to add the Force Outfit Change button column to the Assign tab.
    /// This patches after defs are loaded to insert our column.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class AssignTabPatch
    {
        static AssignTabPatch()
        {
            // Add our column to the Assign tab after all defs are loaded
            AddForceOutfitChangeColumn();
        }
        
        /// <summary>
        /// Adds our custom column after the Outfit column in the Assign tab.
        /// </summary>
        private static void AddForceOutfitChangeColumn()
        {
            var assignTableDef = PawnTableDefOf.Assign;
            if (assignTableDef?.columns == null)
            {
                Log.Error("[ForceOutfitChange] Could not find Assign table def or its columns list.");
                return;
            }
            
            // Check if we've already added the column (in case of hot reload)
            if (assignTableDef.columns.Any(c => c == ForceOutfitChangeDefOf.ForceOutfitChangeButton))
            {
                return;
            }
            
            // Find the outfit column
            int outfitColumnIndex = assignTableDef.columns.FindIndex(c => c.defName == "Outfit");
            if (outfitColumnIndex >= 0)
            {
                // Insert our column right after the outfit column
                assignTableDef.columns.Insert(outfitColumnIndex + 1, ForceOutfitChangeDefOf.ForceOutfitChangeButton);
                
                if (Prefs.DevMode)
                {
                    Log.Message($"[ForceOutfitChange] Added force outfit change column to Assign tab at position {outfitColumnIndex + 1}");
                }
            }
            else
            {
                // If we can't find the outfit column, add it at the end
                assignTableDef.columns.Add(ForceOutfitChangeDefOf.ForceOutfitChangeButton);
                Log.Warning("[ForceOutfitChange] Could not find Outfit column in Assign tab, adding force outfit change column at the end.");
            }
        }
    }
}