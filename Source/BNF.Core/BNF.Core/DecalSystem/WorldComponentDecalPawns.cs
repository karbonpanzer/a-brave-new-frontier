using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace BNF.Core.DecalSystem
{
    public class WorldComponentDecalPawns : WorldComponent
    {
        public static WorldComponentDecalPawns? Instance { get; private set; }

        private HashSet<Pawn> _pawns = new HashSet<Pawn>();
        
        // World Compenent stuff is needed as issues with pawns and their decals persist. Plus this is how VFEM2 handles Heraldics and it is a stripped down version for my usage.
        // Saves/restores the pawn across the game laoding
        public WorldComponentDecalPawns(World world) : base(world) => Instance = this;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref _pawns, "bnfDecalPawns", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                _pawns ??= new HashSet<Pawn>();
        }
        
        // Keeps and pulls the HashSet of pawns known to have decal apparel
        public void Register(Pawn pawn) => _pawns.Add(pawn);

        public void Unregister(Pawn pawn) => _pawns.Remove(pawn);

        public bool HasDecalApparel(Pawn pawn) => _pawns.Contains(pawn);

        public CompEditDecalMarker? GetComp(Pawn pawn)
        {
            if (!_pawns.Contains(pawn) || pawn.apparel == null) return null;
            foreach (var apparel in pawn.apparel.WornApparel)
            {
                var comp = apparel.TryGetComp<CompEditDecalMarker>();
                if (comp != null) return comp;
            }
            return null;
        }
    }
}
