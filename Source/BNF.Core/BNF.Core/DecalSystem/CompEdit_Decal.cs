using Verse;
using UnityEngine;

namespace BNF.Core.DecalSystem
{
    public sealed class CompEditDecalMarker : ThingComp 
    {
        public DecalProfileSet ProfileSet = DecalProfileSet.Default;

        public override void PostExposeData()
        {
            base.PostExposeData();
            
            Scribe_Values.Look(ref ProfileSet.Helmet.Active, "bnfDecalHelmetActive");
            Scribe_Values.Look(ref ProfileSet.Helmet.SymbolPath, "bnfDecalHelmetPath", "");
            Scribe_Values.Look(ref ProfileSet.Helmet.SymbolColor, "bnfDecalHelmetColor", Color.white);
            
            Scribe_Values.Look(ref ProfileSet.Armor.Active, "bnfDecalArmorActive");
            Scribe_Values.Look(ref ProfileSet.Armor.SymbolPath, "bnfDecalArmorPath", "");
            Scribe_Values.Look(ref ProfileSet.Armor.SymbolColor, "bnfDecalArmorColor", Color.white);
        }

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            WorldComponentDecalPawns.Instance?.Register(pawn);
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            foreach (var apparel in pawn.apparel.WornApparel)
            {
                if (apparel.TryGetComp<CompEditDecalMarker>() != null) return;
            }
            WorldComponentDecalPawns.Instance?.Unregister(pawn);
        }
    }

    public sealed class CompPropertiesEditDecalMarker : CompProperties
    {
        public CompPropertiesEditDecalMarker() => compClass = typeof(CompEditDecalMarker);
    }
}
