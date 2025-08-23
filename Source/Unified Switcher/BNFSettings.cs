using Verse;

namespace BNF.StyleSwitcher
{
    public class BNFSettings : ModSettings
    {
        // Master toggles
        public bool EnableDescriptionSwitch = true; 
        public bool EnableTextureSwitch = true; 

        // Mutually-exclusive choices
        public bool UseLoreDescriptions = true; // false = Vanilla
        public bool UseGreyscaleTextures = false; // false = Original

        public override void ExposeData()
        {
            Scribe_Values.Look(ref EnableDescriptionSwitch, "enableDescriptionSwitch", true);
            Scribe_Values.Look(ref EnableTextureSwitch, "enableTextureSwitch", true);

            Scribe_Values.Look(ref UseLoreDescriptions, "UseLoreDescriptions", true);
            Scribe_Values.Look(ref UseGreyscaleTextures, "UseGreyscaleTextures", false);
        }
    }
}
