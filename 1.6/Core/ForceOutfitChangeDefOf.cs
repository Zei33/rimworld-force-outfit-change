using RimWorld;
using Verse;

namespace ForceOutfitChange.Core
{
    /// <summary>
    /// Static references to Defs used by the Force Outfit Change mod.
    /// </summary>
    [DefOf]
    public static class ForceOutfitChangeDefOf
    {
        public static JobDef ForceRemoveApparel;
        
        public static JobDef ForceWear;
        
        public static PawnColumnDef ForceOutfitChangeButton;
        
        static ForceOutfitChangeDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ForceOutfitChangeDefOf));
        }
    }
}