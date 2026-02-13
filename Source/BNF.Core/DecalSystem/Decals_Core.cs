using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using VEF.Graphics;
using Verse;

namespace BNF.Core.DecalSystem
{
    public static class DecalTags
    {
        public const string Active = "DecalActive";
        public const string SymbolPath = "DecalSymbol";
        public const string SymbolColor = "DecalSymbolColor";

        public const string ActiveHead = "DecalActive_Head";
        public const string SymbolPathHead = "DecalSymbol_Head";
        public const string SymbolColorHead = "DecalSymbolColor_Head";
    }

    public struct DecalTagSet
    {
        public readonly string Active;
        public readonly string SymbolPath;
        public readonly string SymbolColor;

        public DecalTagSet(string active, string symbolPath, string symbolColor)
        {
            Active = active;
            SymbolPath = symbolPath;
            SymbolColor = symbolColor;
        }
    }

    public sealed class DecalSymbolDef : Def
    {
        public string Path = "";
    }

    public struct DecalProfile
    {
        public bool Active;
        public string SymbolPath;
        public Color SymbolColor;

        public static DecalProfile Default => new DecalProfile
        {
            Active = false,
            SymbolPath = "",
            SymbolColor = new Color(0.6f, 0.6f, 0.6f, 1f)
        };
    }

    public static class DecalUtil
    {
        public static readonly DecalTagSet BodyTags =
            new DecalTagSet(DecalTags.Active, DecalTags.SymbolPath, DecalTags.SymbolColor);

        public static readonly DecalTagSet HeadTags =
            new DecalTagSet(DecalTags.ActiveHead, DecalTags.SymbolPathHead, DecalTags.SymbolColorHead);

        public static bool IsHumanlikePawn(Pawn? pawn) => pawn?.RaceProps?.Humanlike == true;

        public static bool PawnHasAnyDecalApparel(Pawn? pawn)
        {
            var list = pawn?.apparel?.WornApparel;
            if (list == null) return false;

            for (int i = 0; i < list.Count; i++)
            {
                var a = list[i];
                if (a?.TryGetComp<CompEditDecalMarker>() != null)
                    return true;
            }

            return false;
        }

        public static IEnumerable<Apparel> PawnDecalApparel(Pawn? pawn)
        {
            var list = pawn?.apparel?.WornApparel;
            if (list == null) yield break;

            for (int i = 0; i < list.Count; i++)
            {
                var a = list[i];
                if (a?.TryGetComp<CompEditDecalMarker>() != null)
                    yield return a;
            }
        }

        public static DecalProfile ReadProfileFrom(ILoadReferenceable target) => ReadProfileFrom(target, BodyTags);

        public static DecalProfile ReadProfileFrom(ILoadReferenceable target, DecalTagSet tags)
        {
            bool active = target.GetStringByTag(tags.Active) != null;
            var sp = target.GetStringByTag(tags.SymbolPath);
            var sc = target.GetColorByTag(tags.SymbolColor);

            return new DecalProfile
            {
                Active = active,
                SymbolPath = sp?.value ?? "",
                SymbolColor = sc?.value ?? new Color(0.6f, 0.6f, 0.6f, 1f)
            };
        }

        public static void WriteProfileTo(ILoadReferenceable target, DecalProfile profile) =>
            WriteProfileTo(target, profile, BodyTags);

        public static void WriteProfileTo(ILoadReferenceable target, DecalProfile profile, DecalTagSet tags)
        {
            if (profile.Active)
            {
                TaggedTextPropManager.SetTagItem(target, new TaggedText(tags.Active, "true"));
                TaggedTextPropManager.SetTagItem(target, new TaggedText(tags.SymbolPath, profile.SymbolPath));
                TaggedColorPropManager.SetTagItem(target, new TaggedColor(tags.SymbolColor, profile.SymbolColor));
                return;
            }

            target.RemoveStringTag(tags.Active);
            target.RemoveStringTag(tags.SymbolPath);
            target.RemoveColorTag(tags.SymbolColor);
        }

        public static void WriteSharedProfileTo(ILoadReferenceable target, DecalProfile profile)
        {
            WriteProfileTo(target, profile, BodyTags);
            WriteProfileTo(target, profile, HeadTags);
        }

        private static readonly Dictionary<int, DecalProfile> LiveEditOriginalByPawnId = new Dictionary<int, DecalProfile>();

        public static void BeginLiveEdit(Pawn? pawn)
        {
            if (pawn == null) return;
            int id = pawn.thingIDNumber;
            if (LiveEditOriginalByPawnId.ContainsKey(id)) return;

            LiveEditOriginalByPawnId[id] = ReadProfileFrom(pawn);
        }

        public static void SetLiveEdit(Pawn? pawn, DecalProfile profile)
        {
            if (pawn == null) return;
            BeginLiveEdit(pawn);
            ApplyAndRefresh(pawn, profile);
        }

        public static void EndLiveEdit(Pawn? pawn, bool commit)
        {
            if (pawn == null) return;
            int id = pawn.thingIDNumber;

            if (!LiveEditOriginalByPawnId.TryGetValue(id, out var original))
                return;

            if (!commit)
                ApplyAndRefresh(pawn, original);

            LiveEditOriginalByPawnId.Remove(id);
        }

        public static void ApplyAndRefresh(Pawn? pawn, DecalProfile profile)
        {
            if (pawn == null) return;

            WriteSharedProfileTo(pawn, profile);

            foreach (var a in PawnDecalApparel(pawn))
                WriteSharedProfileTo(a, profile);

            DirtyPawnGraphics(pawn);
        }

        public static void DirtyPawnGraphics(Pawn? pawn)
        {
            pawn?.apparel?.Notify_ApparelChanged();
            pawn?.Drawer?.renderer?.SetAllGraphicsDirty();
        }

        public static List<DecalSymbolDef> AllSymbols() =>
            DefDatabase<DecalSymbolDef>.AllDefsListForReading
                .Where(d => d != null)
                .OrderBy(d => d.label)
                .ToList();
    }
}
