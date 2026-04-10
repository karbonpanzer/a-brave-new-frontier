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
        
        //I wanted a live preview instead of selecting via interface, so this is how it sets up
        public static void SetLiveEditFull(Pawn pawn, DecalProfileSet profileSet)
        {
            WriteProfileSetTo(pawn, profileSet);
            pawn.Drawer.renderer?.SetAllGraphicsDirty();
        }

        public static void EndLiveEdit(Pawn pawn, bool commit, DecalProfileSet original) 
        {
            if (!commit)
            {
                WriteProfileSetTo(pawn, original);
            }
            pawn.Drawer.renderer?.SetAllGraphicsDirty();
        }

        //This is a marker system, needed to find CompEditDecalMarker via WorldComponenet First before falling back if that fails
        private static CompEditDecalMarker? GetMarker(Pawn? pawn)
        {
            if (pawn?.apparel == null) return null;
            
            var registry = WorldComponentDecalPawns.Instance;
            if (registry != null)
            {
                var cached = registry.GetComp(pawn);
                if (cached != null) return cached;
                if (registry.HasDecalApparel(pawn)) registry.Unregister(pawn);
            }
            
            foreach (var apparel in pawn.apparel.WornApparel)
            {
                var comp = apparel.TryGetComp<CompEditDecalMarker>();
                if (comp != null)
                {
                    registry?.Register(pawn);
                    return comp;
                }
            }
            return null;
        }

        //Pawn Checks that are just in case but really are here because I fucked up and had the comp appear on a testing camel
        public static List<DecalSymbolDef> AllSymbols() => DefDatabase<DecalSymbolDef>.AllDefsListForReading;
        public static bool IsHumanlikePawn(Pawn? pawn) => pawn?.RaceProps.Humanlike ?? false;
        public static bool PawnHasAnyDecalApparel(Pawn? pawn) => GetMarker(pawn) != null;
    }
}
