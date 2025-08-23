using Verse;

namespace BNF.StyleSwitcher
{
    [StaticConstructorOnStartup]
    public static class TextureApplierBootstrap
    {
        static TextureApplierBootstrap()
        {
            // Apply at game start based on saved settings
            TextureApplier.ApplyAll(BNFMod.Settings ?? new BNFSettings());
        }
    }

    public static class TextureApplier
    {
        public static void ApplyAll(BNFSettings settings)
        {
            if (settings == null) return;

            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                var ext = def.GetModExtension<BNFTextureExtension>();
                if (ext == null) continue;

                var chosen = settings.UseGreyscaleTextures ? ext.greyscalePath : ext.originalPath;
                if (string.IsNullOrEmpty(chosen)) continue;
                chosen = PathUtil.Normalize(chosen);

                if (def.graphicData != null && !string.IsNullOrEmpty(def.graphicData.texPath))
                    def.graphicData.texPath = chosen;

                if (def.apparel != null && !string.IsNullOrEmpty(def.apparel.wornGraphicPath))
                    def.apparel.wornGraphicPath = chosen;
            }
        }
    }
}