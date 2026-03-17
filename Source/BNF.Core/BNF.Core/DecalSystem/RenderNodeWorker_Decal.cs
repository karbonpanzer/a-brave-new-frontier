using UnityEngine;
using Verse;

namespace BNF.Core.DecalSystem
{
    public class PawnRenderNodeWorkerApparel : PawnRenderNodeWorker
    {
        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            return base.OffsetFor(node, parms, out pivot);
        }

        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            return base.CanDrawNow(node, parms) && DecalUtil.PawnHasAnyDecalApparel(parms.pawn);
        }
    }

    public class PawnRenderNodeWorkerOmniHead : PawnRenderNodeWorkerApparel { }
}