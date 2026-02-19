using Verse;

namespace BNF.Core.DecalSystem
{
    public sealed class CompPropertiesEditDecalMarker : CompProperties
    {
        public CompPropertiesEditDecalMarker() => compClass = typeof(CompEditDecalMarker);
    }

    public sealed class CompEditDecalMarker : ThingComp 
    {
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            
            if (parent is Pawn p)
            {
                DecalUtil.InitializedPawns.Remove(p.thingIDNumber);
            }
            else if (parent?.ParentHolder is Pawn holderPawn)
            {
                DecalUtil.InitializedPawns.Remove(holderPawn.thingIDNumber);
            }
        }
    }
}