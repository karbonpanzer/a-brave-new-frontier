using UnityEngine;
using Verse;

namespace BNF.Core.DecalSystem
{
    public class PawnRenderNodeDecal : PawnRenderNode
    {
        public PawnRenderNodeDecal(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree) 
            : base(pawn, props, tree) { }

        public override Graphic? GraphicFor(Pawn pawn)
        {
            DecalProfile profile = DecalUtil.ReadProfileFrom(pawn);
            var bnfProps = Props as PawnRenderNodePropertiesOmni;
            
            string path = profile.Active ? profile.SymbolPath : GetDefaultPath(pawn);
            Color finalColor = profile.Active ? profile.SymbolColor : (bnfProps?.Color ?? new Color(0.2f, 0.2f, 0.2f));

            if (path.NullOrEmpty()) return null;

            float finalScale = Props.drawData.ScaleFor(pawn);

            return GraphicDatabase.Get<Graphic_Multi>(path, ShaderDatabase.Cutout, Vector2.one * finalScale, finalColor);
        }

        private string GetDefaultPath(Pawn pawn)
        {
            if (Props is PawnRenderNodePropertiesOmni bnfProps && bnfProps.texPaths.Count > 0)
            {
                int seed = pawn.Faction?.loadID ?? pawn.thingIDNumber;
                return bnfProps.texPaths[seed % bnfProps.texPaths.Count];
            }
            return "";
        }
    }
}