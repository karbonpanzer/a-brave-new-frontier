using Verse;

namespace BNF.StyleSwitcher
{
    // Attach to ThingDef via <modExtensions><li Class="BNF.StyleSwitcher.BNFTextureExtension">..</li></modExtensions>
    public class BNFTextureExtension : DefModExtension
    {
        public string originalPath;
        public string greyscalePath;
    }
}