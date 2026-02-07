using Verse;

namespace BNF.Decals
{
    // Marker comp: put this on apparel you want to support decals on.
    // This is intentionally minimal.
    public class CompProperties_EditDecalMarker : CompProperties
    {
        public CompProperties_EditDecalMarker()
        {
            compClass = typeof(CompEditDecalMarker);
        }
    }

    public class CompEditDecalMarker : ThingComp
    {
    }
}
