using Verse;

namespace BNF.StyleSwitcher
{
    public class BNFSettings : ModSettings
    {
        public bool UseLoreDescriptions = true;
        public bool UseGreyscaleTextures = false;

        public void ResetToDefaults()
        {
            UseLoreDescriptions = true;
            UseGreyscaleTextures = false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref UseLoreDescriptions, "UseLoreDescriptions", true);
            Scribe_Values.Look(ref UseGreyscaleTextures, "UseGreyscaleTextures", false);
        }
    }
}
