using Verse;

namespace BNF.StyleSwitcher
{
    [StaticConstructorOnStartup]
    public static class DescriptionApplierBootstrap
    {
        static DescriptionApplierBootstrap()
        {

            var settings = BNFMod.SettingsOrDefault;
            DescriptionApplier.ApplyAll(settings);
        }
    }

    public static class DescriptionApplier
    {
        public static void ApplyAll(BNFSettings settings)
        {
            if (settings == null) return;

            var defs = DefDatabase<ThingDef>.AllDefsListForReading;
            if (defs == null || defs.Count == 0) return;

            for (int i = 0; i < defs.Count; i++)
            {
                ThingDef def = defs[i];
                if (def == null) continue;

                var ext = def.GetModExtension<BNFDescriptionExtension>();
                if (ext == null) continue;

                string newText = settings.UseLoreDescriptions ? ext.loreDesc : ext.vanillaDesc;
                if (!newText.NullOrEmpty())
                    def.description = newText;
            }
        }
    }
}
