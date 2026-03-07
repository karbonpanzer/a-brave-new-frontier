using Verse;
using UnityEngine;

namespace BNF.Core.DecalSystem
{
    public sealed class CompEditDecalMarker : ThingComp 
    {
        public DecalProfile Profile = DecalProfile.Default;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref Profile.Active, "bnfDecalActive");
            Scribe_Values.Look(ref Profile.SymbolPath, "bnfDecalPath", "");
            Scribe_Values.Look(ref Profile.SymbolColor, "bnfDecalColor", Color.white);
        }
    }

    public sealed class CompPropertiesEditDecalMarker : CompProperties
    {
        public CompPropertiesEditDecalMarker() => compClass = typeof(CompEditDecalMarker);
    }
}