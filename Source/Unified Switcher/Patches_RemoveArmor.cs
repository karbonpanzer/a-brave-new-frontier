using System.Collections.Generic;
using System.Linq;
using Verse;

#if HARMONY
using HarmonyLib;
#endif

namespace BNF.StyleSwitcher.Patches
{
#if HARMONY
    [HarmonyPatch]
    public static class Patch_RemoveItems
    {
        // Patch StockGenerator_TraderStock.GenerateThings(IEnumerable<Thing>) by postfixing ref IEnumerable<Thing> __result
        static System.Reflection.MethodBase TargetMethod1()
        {
            var t = AccessTools.TypeByName("StockGenerator_TraderStock");
            if (t == null) return null;
            return AccessTools.Method(t, "GenerateThings");
        }

        static void Postfix1(ref IEnumerable<Thing> __result)
        {
            if (__result == null) return;
            __result = FilterOutRemoved(__result);
        }

        // Patch ThingSetMaker.Generate(ThingSetMakerParams) which usually returns List<Thing>
        static System.Reflection.MethodBase TargetMethod2()
        {
            var t = typeof(ThingSetMaker);
            return AccessTools.Method(t, "Generate", new[] { typeof(ThingSetMakerParams) });
        }

        static void Postfix2(ref List<Thing> __result)
        {
            if (__result == null) return;
            __result = FilterOutRemoved(__result).ToList();
        }

        // Shared filter logic: exclude any Thing whose def has BNFRemovableExtension with removed=true
        private static IEnumerable<Thing> FilterOutRemoved(IEnumerable<Thing> input)
        {
            if (BNFMod.Instance == null || BNFMod.Settings == null) return input;
            var removed = BNFMod.Settings.RemovedArmorGroups;
            if (removed == null || removed.Count == 0) return input;

            // Respect the master toggle: if disabled, do not filter
            if (!BNFMod.Settings.EnableArmorRemoval) return input;

            foreach (var t in input)
            {
                if (t == null) continue;
                var ext = t.def.GetModExtension<BNFRemovableExtension>();
                if (ext != null)
                {
                    string key = string.IsNullOrEmpty(ext.group) ? t.def.defName : ext.group;
                    if (removed.TryGetValue(key, out var isRemoved) && isRemoved)
                        continue; // skip
                }
                yield return t;
            }
        }
    }
#else
    // Harmony not available: provide a no-op stub so the project compiles without adding Harmony.
    public static class Patch_RemoveItems { /* Harmony not present — patch disabled */ }
#endif
}
