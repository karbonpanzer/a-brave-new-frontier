using UnityEngine;
using Verse;

namespace BNF.Core.DecalSystem
{
    public class PawnRenderNodeDecal : PawnRenderNode
    {
        
        private readonly DecalSlot _slot;

        private Graphic? _cachedGraphic;
        private string?  _cachedPath;
        private Color    _cachedColor;

        public PawnRenderNodeDecal(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree)
        {
            _slot = DetermineSlot(props as PawnRenderNodePropertiesOmni);
        }

        public override Graphic? GraphicFor(Pawn pawn)
        {
            var bnfProps = Props as PawnRenderNodePropertiesOmni;
            if (bnfProps == null) return null;
            
            DecalProfile profile = DecalUtil.ReadProfileFrom(pawn, _slot);
            
            string path = profile.Active ? profile.SymbolPath : GetDefaultPath(pawn, bnfProps);
            Color finalColor = profile.Active ? profile.SymbolColor : bnfProps.Color;

            if (path.NullOrEmpty()) return null;

            if (_cachedPath == path && _cachedColor == finalColor)
                return _cachedGraphic;

            _cachedPath    = path;
            _cachedColor   = finalColor;
            _cachedGraphic = GraphicDatabase.Get<Graphic_Multi>(path, ShaderDatabase.Cutout, Vector2.one, finalColor);

            return _cachedGraphic;
        }

        private static DecalSlot DetermineSlot(PawnRenderNodePropertiesOmni? bnfProps)
        {
            if (bnfProps?.ExplicitSlot.HasValue == true)
                return bnfProps.ExplicitSlot.Value;

            if (bnfProps?.parentTagDef != null)
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
