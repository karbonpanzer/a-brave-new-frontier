using UnityEngine;
using Verse;

namespace BNF.Core.DecalSystem
{
    public class PawnRenderNodeWorkerOmniBodyApparel : PawnRenderNodeWorker
    {
        // Position Offset for the Body, ties into the Facing Direction by Bodytype
        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            Vector3 result = base.OffsetFor(node, parms, out pivot);

            if (parms.pawn == null || !(node.Props is PawnRenderNodePropertiesOmni bnfProps))
                return result;

            var bodyType = parms.pawn.story?.bodyType;
            if (bodyType == null) return result;
            
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
        
        //Needed to avoid the fucking Comp showing up on pawns not wearing Carapace Helmet/Armor
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            return base.CanDrawNow(node, parms) && DecalUtil.PawnHasAnyDecalApparel(parms.pawn);
        }
    }
    // The Headware functions practically the same, all it needs is to be set to ApparelHead in XML
    public class PawnRenderNodeWorkerHeadware : PawnRenderNodeWorkerOmniBodyApparel { }
}