using UnityEngine;
using Verse;

namespace BNF.Core.DecalSystem
{
    public class PawnRenderNodeDecal(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
        : PawnRenderNode(pawn, props, tree)
    {
        public override Graphic? GraphicFor(Pawn pawn)
        {
            var bnfProps = Props as PawnRenderNodePropertiesOmni;
            if (bnfProps == null) return null;
            
            DecalSlot slot = DetermineSlot(bnfProps);
            DecalProfile profile = DecalUtil.ReadProfileFrom(pawn, slot);
            
            string path = profile.Active ? profile.SymbolPath : GetDefaultPath(pawn, bnfProps);
            Color finalColor = profile.Active ? profile.SymbolColor : (bnfProps.Color);

            if (path.NullOrEmpty()) return null;
            
            return GraphicDatabase.Get<Graphic_Multi>(path, ShaderDatabase.Cutout, Vector2.one, finalColor);
        }

        private DecalSlot DetermineSlot(PawnRenderNodePropertiesOmni bnfProps)
        {
            if (bnfProps.ExplicitSlot.HasValue)
                return bnfProps.ExplicitSlot.Value;

            if (bnfProps.parentTagDef != null)
            {
                string tagName = bnfProps.parentTagDef.defName;
                if (tagName.Contains("Head") || tagName.Contains("Headgear") || tagName.Contains("Helmet"))
                    return DecalSlot.Helmet;
            }

            return DecalSlot.Armor;
        }

        private string GetDefaultPath(Pawn pawn, PawnRenderNodePropertiesOmni bnfProps)
        {
            if (bnfProps.texPaths != null && bnfProps.texPaths.Count > 0)
            {
                int seed = pawn.Faction?.loadID ?? pawn.thingIDNumber;
                return bnfProps.texPaths[seed % bnfProps.texPaths.Count];
            }
            return "";
        }
    }
}