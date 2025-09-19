using Verse;

namespace BNF.StyleSwitcher
{
    /// <summary>
    /// Mod settings for BNF style switching.
    /// Defaults: lore descriptions, original textures.
    /// </summary>
    public class BNFSettings : ModSettings
    {
        // Description switching
        public bool EnableDescriptionSwitch = true;
        public bool UseLoreDescriptions = true; // default: lore descriptions

        // Texture switching
        public bool EnableTextureSwitch = true;
        public bool UseGreyscaleTextures = false; // default: original textures (false)

        /// <summary>
        /// Reset to sensible defaults used by the mod.
        /// </summary>
        public void ResetToDefaults()
        {
            EnableDescriptionSwitch = true;
            UseLoreDescriptions = true;
            EnableTextureSwitch = true;
            UseGreyscaleTextures = false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref EnableDescriptionSwitch, "EnableDescriptionSwitch", true);
            Scribe_Values.Look(ref UseLoreDescriptions, "UseLoreDescriptions", true);

            Scribe_Values.Look(ref EnableTextureSwitch, "EnableTextureSwitch", true);
            Scribe_Values.Look(ref UseGreyscaleTextures, "UseGreyscaleTextures", false);
        }
    }
}
