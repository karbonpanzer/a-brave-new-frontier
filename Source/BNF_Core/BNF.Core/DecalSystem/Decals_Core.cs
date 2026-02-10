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
        public readonly string Path = "";
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

        public static List<Color> FixedPalette_LargeSpread()
        {
            return new List<Color>
            {
                new Color(0.05f, 0.05f, 0.05f),
                new Color(0.10f, 0.10f, 0.10f),
                new Color(0.15f, 0.15f, 0.15f),
                new Color(0.22f, 0.22f, 0.22f),
                new Color(0.30f, 0.30f, 0.30f),
                new Color(0.40f, 0.40f, 0.40f),
                new Color(0.55f, 0.55f, 0.55f),
                new Color(0.70f, 0.70f, 0.70f),
                new Color(0.85f, 0.85f, 0.85f),
                new Color(0.95f, 0.95f, 0.95f),

                new Color(0.25f, 0.05f, 0.05f),
                new Color(0.40f, 0.08f, 0.08f),
                new Color(0.55f, 0.10f, 0.10f),
                new Color(0.70f, 0.12f, 0.12f),
                new Color(0.85f, 0.15f, 0.15f),
                new Color(0.90f, 0.30f, 0.30f),
                new Color(0.95f, 0.45f, 0.45f),
                new Color(0.85f, 0.20f, 0.45f),
                new Color(0.75f, 0.15f, 0.55f),
                new Color(0.60f, 0.10f, 0.45f),

                new Color(0.35f, 0.18f, 0.05f),
                new Color(0.55f, 0.25f, 0.06f),
                new Color(0.75f, 0.35f, 0.07f),
                new Color(0.90f, 0.45f, 0.08f),
                new Color(0.95f, 0.60f, 0.12f),
                new Color(0.95f, 0.75f, 0.18f),
                new Color(0.95f, 0.85f, 0.28f),
                new Color(0.85f, 0.80f, 0.20f),
                new Color(0.70f, 0.65f, 0.18f),
                new Color(0.55f, 0.50f, 0.15f),

                new Color(0.06f, 0.20f, 0.08f),
                new Color(0.08f, 0.30f, 0.10f),
                new Color(0.10f, 0.45f, 0.14f),
                new Color(0.12f, 0.60f, 0.18f),
                new Color(0.15f, 0.75f, 0.22f),
                new Color(0.20f, 0.85f, 0.30f),
                new Color(0.35f, 0.85f, 0.45f),
                new Color(0.20f, 0.55f, 0.35f),
                new Color(0.15f, 0.40f, 0.25f),
                new Color(0.10f, 0.28f, 0.18f),

                new Color(0.06f, 0.10f, 0.25f),
                new Color(0.08f, 0.14f, 0.40f),
                new Color(0.10f, 0.18f, 0.55f),
                new Color(0.12f, 0.25f, 0.70f),
                new Color(0.16f, 0.38f, 0.85f),
                new Color(0.22f, 0.55f, 0.95f),
                new Color(0.20f, 0.75f, 0.95f),
                new Color(0.15f, 0.85f, 0.85f),
                new Color(0.10f, 0.65f, 0.65f),
                new Color(0.10f, 0.45f, 0.55f),

                new Color(0.20f, 0.10f, 0.30f),
                new Color(0.30f, 0.12f, 0.45f),
                new Color(0.45f, 0.18f, 0.65f),
                new Color(0.60f, 0.30f, 0.80f),
                new Color(0.75f, 0.50f, 0.90f),

                new Color(0.18f, 0.10f, 0.06f),
                new Color(0.30f, 0.18f, 0.10f),
                new Color(0.45f, 0.28f, 0.16f),
                new Color(0.35f, 0.35f, 0.18f),
                new Color(0.22f, 0.26f, 0.22f),
            };
        }

        public static string ColorToHex(Color c)
        {
            byte r = (byte)Mathf.Clamp(Mathf.RoundToInt(c.r * 255f), 0, 255);
            byte g = (byte)Mathf.Clamp(Mathf.RoundToInt(c.g * 255f), 0, 255);
            byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(c.b * 255f), 0, 255);
            byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(c.a * 255f), 0, 255);

            return a >= 254 ? $"#{r:X2}{g:X2}{b:X2}" : $"#{r:X2}{g:X2}{b:X2}{a:X2}";
        }

        public static bool TryParseHexColor(string raw, out Color parsed)
        {
            parsed = Color.white;
            if (raw.NullOrEmpty()) return false;

            string s = raw.Trim();
            if (!s.StartsWith("#")) s = "#" + s;

            return ColorUtility.TryParseHtmlString(s, out parsed);
        }
    }
}
