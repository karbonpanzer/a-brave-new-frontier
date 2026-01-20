using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using VEF.Graphics;

namespace BNF.Graphics
{
    public class PawnRenderNodeProperties_OmniBNF : PawnRenderNodeProperties_Omni
    {
        public Dictionary<BodyTypeDef, Vector3> bodyTypeOffsets;
        public Dictionary<Rot4, Dictionary<BodyTypeDef, Vector3>> bodyTypeOffsetsByFacing;
    }
}
