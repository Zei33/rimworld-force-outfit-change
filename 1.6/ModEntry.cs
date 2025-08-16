using HarmonyLib;
using Verse;

namespace ModTemplate
{
    /// <summary>
    /// Main mod entry point for the Mod Template mod.
    /// Handles mod initialization and Harmony patching for mod template.
    /// </summary>
    public class ModTemplateMod : Mod
    {   
        /// <summary>
        /// The Harmony instance used for applying patches to the base game.
        /// </summary>
        private readonly Harmony harmony;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModTemplateMod"/> class.
        /// Sets up Harmony patches and logs successful initialization.
        /// </summary>
        /// <param name="pack">The mod content pack containing mod information and assets.</param>
        public ModTemplateMod(ModContentPack pack) : base(pack)
        {
            harmony = new Harmony("com.zei33.modtemplate");
            harmony.PatchAll();

            Log.Message("[Mod Template] Loaded version 1.0 successfully.");
        }
    }
}