using Verse;

namespace BNF.StyleSwitcher
{
    [StaticConstructorOnStartup]
    public static class DescriptionApplierBootstrap
    {
        static DescriptionApplierBootstrap()
        {
            DescriptionApplier.ApplyAll(BNFMod.Settings ?? new BNFSettings());
        }
    }

    public static class DescriptionApplier
    {
        public static void ApplyAll(BNFSettings settings)
        {
            if (settings == null) return;

            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                var ext = def.GetModExtension<BNFDescriptionExtension>();
                if (ext == null) continue;

                var newText = settings.UseLoreDescriptions ? ext.loreDesc : ext.vanillaDesc;
                if (!string.IsNullOrEmpty(newText))
                    def.description = newText;
            }
        }
    }
}
