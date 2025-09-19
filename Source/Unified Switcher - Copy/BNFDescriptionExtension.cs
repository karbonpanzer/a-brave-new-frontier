using Verse;

namespace BNF.StyleSwitcher
{
    // Attach to ThingDef via <modExtensions><li Class="BNF.StyleSwitcher.BNFDescriptionExtension">..</li></modExtensions>
    public class BNFDescriptionExtension : DefModExtension
    {
        public string vanillaDesc;
        public string loreDesc;
    }
}