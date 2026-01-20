using RimWorld;
using UnityEngine;
using Verse;
using VEF.Graphics;

namespace BNF.Graphics
{
    public class PawnRenderNodeWorker_OmniBodyApparel_BNF
        : PawnRenderNodeWorker_OmniBodyApparel
    {
        public override Vector3 OffsetFor(
            PawnRenderNode n,
            PawnDrawParms parms,
            out Vector3 pivot)
        {
            Vector3 result = base.OffsetFor(n, parms, out pivot);

            var props = n.Props as PawnRenderNodeProperties_OmniBNF;
            if (props == null)
                return result;

            var bodyType = parms.pawn?.story?.bodyType;
            if (bodyType == null)
                return result;

            if (props.bodyTypeOffsetsByFacing != null &&
                props.bodyTypeOffsetsByFacing.TryGetValue(parms.facing, out var facingMap) &&
                facingMap != null &&
                facingMap.TryGetValue(bodyType, out var facingOffset))
            {
                result += facingOffset;
                return result;
            }

            if (props.bodyTypeOffsets != null &&
                props.bodyTypeOffsets.TryGetValue(bodyType, out var globalOffset))
            {
                result += globalOffset;
            }

            return result;
        }
    }
}
