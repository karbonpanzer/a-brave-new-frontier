using System.Collections.Generic;
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
            var bnfProps = Props as PawnRenderNodePropertiesOmniBnf;
            
            string path = profile.Active ? profile.SymbolPath : GetDefaultPath(pawn);
            
            Color finalColor = profile.Active ? profile.SymbolColor : (bnfProps?.color ?? new Color(0.2f, 0.2f, 0.2f));

            if (path.NullOrEmpty()) return null;

            return GraphicDatabase.Get<Graphic_Multi>(path, ShaderDatabase.Cutout, Vector2.one * Props.drawData.scale, finalColor);
        }

        private string GetDefaultPath(Pawn pawn)
        {
            if (Props is PawnRenderNodePropertiesOmniBnf bnfProps && bnfProps.texPaths.Count > 0)
            {
                int seed = pawn.Faction?.loadID ?? pawn.thingIDNumber;
                return bnfProps.texPaths[seed % bnfProps.texPaths.Count];
            }
            return "";
        }
    }

    public static class DecalUtil
    {
        public static DecalProfile ReadProfileFrom(Pawn pawn)
        {
            var comp = GetMarker(pawn);
            if (comp != null) return comp.Profile;
            return default;
        }

        public static void WriteProfileTo(Pawn pawn, DecalProfile profile)
        {
            var comp = GetMarker(pawn);
            comp?.Profile = profile;
        }

        private static CompEditDecalMarker? GetMarker(Pawn? pawn)
        {
            if (pawn?.apparel == null) return null;
            foreach (var apparel in pawn.apparel.WornApparel)
            {
                var comp = apparel.TryGetComp<CompEditDecalMarker>();
                if (comp != null) return comp;
            }
            return null;
        }

        public static void SetLiveEdit(Pawn pawn, DecalProfile profile)
        {
            WriteProfileTo(pawn, profile);
            pawn.Drawer?.renderer?.SetAllGraphicsDirty();
        }

        public static void BeginLiveEdit(Pawn pawn) { }
        public static void EndLiveEdit(Pawn pawn, bool commit) { }
        public static List<DecalSymbolDef> AllSymbols() => DefDatabase<DecalSymbolDef>.AllDefsListForReading;
        public static bool IsHumanlikePawn(Pawn? pawn) => pawn?.RaceProps?.Humanlike ?? false;
        public static bool PawnHasAnyDecalApparel(Pawn pawn) => GetMarker(pawn) != null;
    }
}