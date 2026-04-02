using System.Collections.Generic;
using Verse;

namespace BNF.Core.DecalSystem
{
    public static class DecalUtil
    {
        public static DecalProfileSet ReadProfileSetFrom(Pawn pawn)
        {
            var comp = GetMarker(pawn);
            return (comp != null) ? comp.ProfileSet : DecalProfileSet.Default;
        }

        public static DecalProfile ReadProfileFrom(Pawn pawn, DecalSlot slot)
        {
            var comp = GetMarker(pawn);
            if (comp == null) return DecalProfile.Default;
            return (slot == DecalSlot.Helmet) ? comp.ProfileSet.Helmet : comp.ProfileSet.Armor;
        }

        public static void WriteProfileSetTo(Pawn pawn, DecalProfileSet profileSet)
        {
            var comp = GetMarker(pawn);
            if (comp != null) comp.ProfileSet = profileSet;
        }

        public static void SetLiveEditFull(Pawn pawn, DecalProfileSet profileSet)
        {
            WriteProfileSetTo(pawn, profileSet);
            pawn.Drawer?.renderer?.SetAllGraphicsDirty();
        }

        public static void EndLiveEdit(Pawn pawn, bool commit, DecalProfileSet original) 
        {
            if (!commit)
            {
                WriteProfileSetTo(pawn, original);
            }
            pawn.Drawer?.renderer?.SetAllGraphicsDirty();
        }

        private static CompEditDecalMarker? GetMarker(Pawn? pawn)
        {
            if (pawn?.apparel == null) return null;
            var worn = pawn.apparel.WornApparel;
            for (int i = 0; i < worn.Count; i++)
            {
                var comp = worn[i].TryGetComp<CompEditDecalMarker>();
                if (comp != null) return comp;
            }
            return null;
        }

        public static List<DecalSymbolDef> AllSymbols() => DefDatabase<DecalSymbolDef>.AllDefsListForReading;
        public static bool IsHumanlikePawn(Pawn? pawn) => pawn?.RaceProps?.Humanlike ?? false;
        public static bool PawnHasAnyDecalApparel(Pawn? pawn) => GetMarker(pawn) != null;
    }
}