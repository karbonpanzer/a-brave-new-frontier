using Verse;

namespace BNF.Core.DescriptionSwitcher
{
    public class BnfSettings : ModSettings
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