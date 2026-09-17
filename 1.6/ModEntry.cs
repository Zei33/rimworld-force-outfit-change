using HarmonyLib;
using Verse;
using ForceOutfitChange.Core;

namespace ForceOutfitChange
{
    /// <summary>
    /// Main mod entry point for the Force Outfit Change mod.
    /// Handles mod initialization and Harmony patching for Force Outfit Change.
    /// </summary>
    public class ForceOutfitChangeMod : Mod
    {   
        /// <summary>
        /// The Harmony instance used for applying patches to the base game.
        /// </summary>
        private readonly Harmony harmony;

        /// <summary>
        /// Initializes a new instance of the <see cref="ForceOutfitChangeMod"/> class.
        /// Sets up Harmony patches and logs successful initialization.
        /// </summary>
        /// <param name="pack">The mod content pack containing mod information and assets.</param>
        public ForceOutfitChangeMod(ModContentPack pack) : base(pack)
        {
            harmony = new Harmony("com.zei33.forceoutfitchange");
            harmony.PatchAll();

            // Clear any stale markers from previous sessions
            ForceOutfitChangeMarker.ClearAllMarkers();

            Log.Message("[Force Outfit Change] Loaded successfully with Harmony patches applied.");
        }
    }
}