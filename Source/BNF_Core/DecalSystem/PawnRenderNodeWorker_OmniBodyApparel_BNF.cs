using RimWorld;
using UnityEngine;
using Verse;
using VEF.Graphics;

namespace BNF.Graphics
{
    public class PawnRenderNodeWorker_OmniBodyApparel_BNF : PawnRenderNodeWorker_OmniBodyApparel
    {
        public override Vector3 OffsetFor(PawnRenderNode n, PawnDrawParms parms, out Vector3 pivot)
        {
            var result = base.OffsetFor(n, parms, out pivot);

            if (n?.Props is not PawnRenderNodeProperties_OmniBNF props)
                return result;

            var pawn = parms.pawn;
            var bodyType = pawn?.story?.bodyType;
            if (bodyType == null)
                return result;

            // Normalize XML-facing rows into lookups once, on first use.
            props.EnsureBodyTypeOffsetsByFacingBuilt();

            // Facing offsets take priority over global body-type offsets.
            if (props.bodyTypeOffsetsByFacing.TryGetValue(parms.facing, out var facingMap) &&
                facingMap.TryGetValue(bodyType, out var facingOffset))
            {
                return result + facingOffset;
            }

            if (props.bodyTypeOffsets.TryGetValue(bodyType, out var globalOffset))
            {
                result += globalOffset;
            }

            return result;
        }
    }
}
