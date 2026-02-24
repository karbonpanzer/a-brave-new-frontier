using Verse;

namespace BNF.Core.DescriptionSwitcher
{
    [StaticConstructorOnStartup]
    public static class DescriptionApplierBootstrap
    {
        static DescriptionApplierBootstrap()
        {
            DescriptionApplier.ApplyAll(BnfMod.Settings);
            Log.Message("[BNF] Description Switcher loaded");
        }
    }

    public static class DescriptionApplier
    {
        public static void ApplyAll(BnfSettings? settings)
        {
            if (settings == null) return;

            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                var ext = def.GetModExtension<BnfDescriptionExtension>();
                if (ext == null) continue;

                var newText = settings.UseLoreDescriptions ? ext.LoreDesc : ext.VanillaDesc;
                if (!string.IsNullOrEmpty(newText))
                    def.description = newText;
            }
        }
    }
}