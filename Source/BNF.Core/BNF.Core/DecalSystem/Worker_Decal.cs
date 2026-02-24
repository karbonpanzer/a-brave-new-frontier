using UnityEngine;
using Verse;
using RimWorld;

namespace BNF.Core.DecalSystem
{
    public class PawnRenderNodeWorkerOmniBodyApparelBnf : PawnRenderNodeWorker
    {
        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            Vector3 result = base.OffsetFor(node, parms, out pivot);
            

            if (parms.pawn == null || !(node.Props is PawnRenderNodePropertiesOmniBnf bnfProps))
                return result;

            var bodyType = parms.pawn.story?.bodyType;
            if (bodyType == null) return result;
            
            bnfProps.EnsureBodyTypeOffsetsByFacingBuilt();
            
            if (bnfProps.BodyTypeOffsetsByFacing.TryGetValue(parms.facing, out var facingMap) &&
                facingMap.TryGetValue(bodyType, out var facingOffset))
            {
                result += facingOffset;
            }
            
            else if (bnfProps.BodyTypeOffsets.TryGetValue(bodyType, out var globalOffset))
            {
                result += globalOffset;
            }

            return result;
        }

        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            return base.CanDrawNow(node, parms) && DecalUtil.PawnHasAnyDecalApparel(parms.pawn);
        }
    }

    public class PawnRenderNodeWorkerOmniHeadApparelBnf : PawnRenderNodeWorkerOmniBodyApparelBnf { }
}