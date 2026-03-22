using UnityEngine;
using Verse;

namespace BNF.Core.DecalSystem
{
    public class PawnRenderNodeWorkerApparel : PawnRenderNodeWorker
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {

            return base.CanDrawNow(node, parms) && DecalUtil.PawnHasAnyDecalApparel(parms.pawn);
        }
    }

    public class PawnRenderNodeWorkerHeadware : PawnRenderNodeWorkerApparel { }
}