using Verse;

namespace BNF.Decals
{
    public sealed class CompProperties_EditDecalMarker : CompProperties
    {
        public CompProperties_EditDecalMarker() => compClass = typeof(CompEditDecalMarker);
    }

    public sealed class CompEditDecalMarker : ThingComp { }
}
