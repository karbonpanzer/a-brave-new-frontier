using Verse;

namespace BNF.StyleSwitcher
{
    public class BNFSettings : ModSettings
    {
        public bool UseLoreDescriptions = true;

        public void ResetToDefaults()
        {
            UseLoreDescriptions = true;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref UseLoreDescriptions, "UseLoreDescriptions", true);
        }
    }
}
