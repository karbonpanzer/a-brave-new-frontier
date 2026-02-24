using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace BNF.Core.DecalSystem
{
    [StaticConstructorOnStartup]
    public static class DecalBootstrap
    {
        static DecalBootstrap()
        {
            try
            {
                new Harmony("BNF.Decals").PatchAll();
                Log.Message("[BNF] Decal System loaded successfully.");
            }
            catch (System.Exception e)
            {
                Log.Error("[BNF] Decal System failed to load:\n" + e);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class PatchPawnGetGizmosDecals
    {
        public static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            if (__instance.Faction != Faction.OfPlayerSilentFail || 
                !DecalUtil.IsHumanlikePawn(__instance) || 
                !DecalUtil.PawnHasAnyDecalApparel(__instance)) return;
        
            __result = __result.Concat(new[] { CreateDecalGizmo(__instance) });
        }

        private static Gizmo CreateDecalGizmo(Pawn pawn)
        {
            return new Command_Action
            {
                defaultLabel = "BNF_StyleDecalsGizmo".Translate(pawn.LabelCap),
                defaultDesc = "BNF_StyleDecalsDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/CustomizeDecal"),
                action = () => Find.WindowStack.Add(new DialogEditDecals(pawn))
            };
        }
    }
}