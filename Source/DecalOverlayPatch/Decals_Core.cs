using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using VEF.Graphics;

namespace BNF.Decals
{
    public static class DecalTags
    {
        // Must match your XML exactly
        public const string ACTIVE = "DecalActive";
        public const string SYMBOL_PATH = "DecalSymbol";
        public const string SYMBOL_CLR = "DecalSymbolColor";
    }

    public sealed class DecalSymbolDef : Def
    {
        public string path = "";
        public float weight = 1f;
        public bool blankType = false;
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
        // ------------------------------------------------------------
        // Eligibility
        // ------------------------------------------------------------

        public static bool IsHumanlikePawn(Pawn pawn)
        {
            return pawn != null && pawn.RaceProps != null && pawn.RaceProps.Humanlike;
        }

        public static bool PawnHasAnyDecalApparel(Pawn pawn)
        {
            if (pawn?.apparel?.WornApparel == null) return false;

            for (int i = 0; i < pawn.apparel.WornApparel.Count; i++)
            {
                var a = pawn.apparel.WornApparel[i];
                if (a != null && a.TryGetComp<CompEditDecalMarker>() != null)
                    return true;
            }
            return false;
        }

        public static IEnumerable<Apparel> PawnDecalApparel(Pawn pawn)
        {
            if (pawn?.apparel?.WornApparel == null) yield break;

            foreach (var a in pawn.apparel.WornApparel)
            {
                if (a != null && a.TryGetComp<CompEditDecalMarker>() != null)
                    yield return a;
            }
        }

        // ------------------------------------------------------------
        // Read / write tagged data (VEF tagged props)
        // ------------------------------------------------------------

        public static DecalProfile ReadProfileFrom(ILoadReferenceable target)
        {
            bool active = target.GetStringByTag(DecalTags.ACTIVE) != null;

            var sp = target.GetStringByTag(DecalTags.SYMBOL_PATH);
            var sc = target.GetColorByTag(DecalTags.SYMBOL_CLR);

            return new DecalProfile
            {
                Active = active,
                SymbolPath = sp?.value ?? "",
                SymbolColor = sc?.value ?? new Color(0.6f, 0.6f, 0.6f, 1f),
            };
        }

        public static void WriteProfileTo(ILoadReferenceable target, DecalProfile profile)
        {
            if (profile.Active)
            {
                TaggedTextPropManager.SetTagItem(target, new TaggedText(DecalTags.ACTIVE, "1"));
                TaggedTextPropManager.SetTagItem(target, new TaggedText(DecalTags.SYMBOL_PATH, profile.SymbolPath ?? ""));
                TaggedColorPropManager.SetTagItem(target, new TaggedColor(DecalTags.SYMBOL_CLR, profile.SymbolColor));
            }
            else
            {
                // Remove tags so the XML tagRequirements fails and fallback texPaths are used
                target.RemoveStringTag(DecalTags.ACTIVE);
                target.RemoveStringTag(DecalTags.SYMBOL_PATH);
                target.RemoveColorTag(DecalTags.SYMBOL_CLR);
            }
        }

        public static void ApplyProfileToPawnAndDecalApparel(Pawn pawn, DecalProfile profile)
        {
            WriteProfileTo(pawn, profile);

            // Mirror to decal apparel as well (keeps behavior stable across contexts)
            foreach (var a in PawnDecalApparel(pawn))
                WriteProfileTo(a, profile);

            DirtyPawnGraphics(pawn);
        }

        public static void DirtyPawnGraphics(Pawn pawn)
        {
            if (pawn == null) return;
            pawn.apparel?.Notify_ApparelChanged();
            pawn.Drawer?.renderer?.SetAllGraphicsDirty();
        }

        // ------------------------------------------------------------
        // Symbol defs
        // ------------------------------------------------------------

        public static List<DecalSymbolDef> AllSymbols()
        {
            return DefDatabase<DecalSymbolDef>.AllDefsListForReading
                .Where(d => d != null)
                .OrderBy(d => d.label)
                .ToList();
        }

        // ------------------------------------------------------------
        // Fixed palette + hex helpers
        // ------------------------------------------------------------

        /// <summary>
        /// Fixed, stable, big spread palette.
        /// 60 colors total. With 5 rows, this becomes 12 columns.
        /// </summary>
        public static List<Color> FixedPalette_LargeSpread()
        {
            return new List<Color>
            {
                // Neutrals (10)
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

                // Reds / Pinks (10)
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

                // Oranges / Yellows (10)
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

                // Greens (10)
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

                // Blues / Cyans (10)
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

                // Purples / Browns / Mil tones (10)
                new Color(0.20f, 0.10f, 0.30f),
                new Color(0.30f, 0.12f, 0.45f),
                new Color(0.45f, 0.18f, 0.65f),
                new Color(0.60f, 0.30f, 0.80f),
                new Color(0.75f, 0.50f, 0.90f),

                new Color(0.18f, 0.10f, 0.06f),
                new Color(0.30f, 0.18f, 0.10f),
                new Color(0.45f, 0.28f, 0.16f),
                new Color(0.35f, 0.35f, 0.18f), // olive drab
                new Color(0.22f, 0.26f, 0.22f), // muted green-grey
            };
        }

        public static string ColorToHex(Color c)
        {
            byte r = (byte)Mathf.Clamp(Mathf.RoundToInt(c.r * 255f), 0, 255);
            byte g = (byte)Mathf.Clamp(Mathf.RoundToInt(c.g * 255f), 0, 255);
            byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(c.b * 255f), 0, 255);
            byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(c.a * 255f), 0, 255);

            if (a >= 254) return $"#{r:X2}{g:X2}{b:X2}";
            return $"#{r:X2}{g:X2}{b:X2}{a:X2}";
        }

        /// <summary>
        /// Accepts: RRGGBB / RRGGBBAA / #RRGGBB / #RRGGBBAA
        /// </summary>
        public static bool TryParseHexColor(string raw, out Color parsed)
        {
            parsed = Color.white;

            if (raw.NullOrEmpty()) return false;

            string s = raw.Trim();
            if (!s.StartsWith("#"))
                s = "#" + s;

            return ColorUtility.TryParseHtmlString(s, out parsed);
        }
    }
}
