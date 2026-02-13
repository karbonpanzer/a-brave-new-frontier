using Verse;

namespace BNF.Core.DecalSystem
{
    public sealed class CompPropertiesEditDecalMarker : CompProperties
    {
        public CompPropertiesEditDecalMarker() => compClass = typeof(CompEditDecalMarker);
    }

    public sealed class CompEditDecalMarker : ThingComp { }
}
