using UnityEngine;
using VEF.Graphics;
using Verse;

namespace BNF.Core.DecalSystem
{
    public class PawnRenderNodeWorkerOmniBodyApparelBnf : PawnRenderNodeWorker_OmniBodyApparel
    {
        public override Vector3 OffsetFor(PawnRenderNode? n, PawnDrawParms parms, out Vector3 pivot)
        {
            var result = base.OffsetFor(n, parms, out pivot);
            
            if (parms.pawn == null || parms.pawn.Destroyed || n?.Props is not PawnRenderNodePropertiesOmniBnf props)
                return result;

            var pawn = parms.pawn;
            
            var bodyType = pawn?.story?.bodyType;
            if (bodyType == null)
                return result;
            
            props.EnsureBodyTypeOffsetsByFacingBuilt();
            
            if (props.BodyTypeOffsetsByFacing.TryGetValue(parms.facing, out var facingMap) &&
                facingMap.TryGetValue(bodyType, out var facingOffset))
            {
                return result + facingOffset;
            }

            if (props.BodyTypeOffsets.TryGetValue(bodyType, out var globalOffset))
            {
                result += globalOffset;
            }

            return result;
        }
    }
}