using System.Collections.Generic;
using Verse;

namespace BNF.Core.DecalSystem
{
    public static class DecalUtil
    {
        public static DecalProfile ReadProfileFrom(Pawn pawn)
        {
            var comp = GetMarker(pawn);
            return (comp != null) ? comp.Profile : DecalProfile.Default;
        }

        public static void WriteProfileTo(Pawn pawn, DecalProfile profile)
        {
            var comp = GetMarker(pawn);
            if (comp != null)
            {
                comp.Profile = profile;
            }
        }

        private static CompEditDecalMarker? GetMarker(Pawn? pawn)
        {
            if (pawn?.apparel == null) return null;

            var wornApparel = pawn.apparel.WornApparel;
            for (int i = 0; i < wornApparel.Count; i++)
            {
                var comp = wornApparel[i].TryGetComp<CompEditDecalMarker>();
                if (comp != null) return comp;
            }
            return null;
        }

        public static void SetLiveEdit(Pawn pawn, DecalProfile profile)
        {
            WriteProfileTo(pawn, profile);
            pawn.Drawer?.renderer?.SetAllGraphicsDirty();
        }

        public static void BeginLiveEdit(Pawn pawn) 
        { 
        }

        public static void EndLiveEdit(Pawn pawn, bool commit) 
        {
            if (!commit)
            {
                pawn.Drawer?.renderer?.SetAllGraphicsDirty();
            }
        }

        public static List<DecalSymbolDef> AllSymbols() 
        {
            return DefDatabase<DecalSymbolDef>.AllDefsListForReading;
        }

        public static bool IsHumanlikePawn(Pawn? pawn) 
        {
            return pawn?.RaceProps != null && pawn.RaceProps.Humanlike;
        }

        public static bool PawnHasAnyDecalApparel(Pawn pawn) 
        {
            return GetMarker(pawn) != null;
        }
    }
}