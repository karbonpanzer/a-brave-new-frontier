using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace BNF.Core.DecalSystem
{
    public class PawnRenderNodePropertiesOmni : PawnRenderNodeProperties
    {
        public new List<string> texPaths = new List<string>(); 
        
        public Color Color = new Color(0.2f, 0.2f, 0.2f); 

        public PawnRenderNodePropertiesOmni()
        {
            nodeClass = typeof(PawnRenderNodeDecal);
            workerClass = typeof(PawnRenderNodeWorkerApparel);
        }
    }
}