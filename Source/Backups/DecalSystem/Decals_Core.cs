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
        // Body (existing, must match your XML exactly)
        public const string ACTIVE = "DecalActive";
        public const string SYMBOL_PATH = "DecalSymbol";
        public const string SYMBOL_CLR = "DecalSymbolColor";

        // Head (new, for helmets)
        public const string ACTIVE_HEAD = "DecalActive_Head";
        public const string SYMBOL_PATH_HEAD = "DecalSymbol_Head";
        public const string SYMBOL_CLR_HEAD = "DecalSymbolColor_Head";
    }

    public struct DecalTagSet
    {
        public string Active;
        public string SymbolPath;
        public string SymbolColor;

        public DecalTagSet(string active, string symbolPath, string symbolColor)
        {
            Active = active;
            SymbolPath = symbolPath;
            SymbolColor = symbolColor;
        }
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
        public static readonly DecalTagSet TagBody =
            new DecalTagSet(DecalTags.ACTIVE, DecalTags.SYMBOL_PATH, DecalTags.SYMBOL_CLR);

        public static readonly DecalTagSet TagHead =
            new DecalTagSet(DecalTags.ACTIVE_HEAD, DecalTags.SYMBOL_PATH_HEAD, DecalTags.SYMBOL_CLR_HEAD);

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
                if (a == null) continue;

                if (a.TryGetComp<CompEditDecalMarker>() != null)
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

        // Returns worn apparel explicitly marked as decal-supporting via CompEditDecalMarker.
        // If you want helmets to be eligible, add the marker comp to those helmet ThingDefs too.
        public static IEnumerable<Apparel> PawnDecalApparelAndHelmets(Pawn pawn)
        {
            if (pawn?.apparel?.WornApparel == null) yield break;

            foreach (var a in pawn.apparel.WornApparel)
            {
                if (a == null) continue;

                if (a.TryGetComp<CompEditDecalMarker>() != null)
                    yield return a;
            }
        }

        // Write the SAME profile into both tag sets.
        // This keeps head and body decals shared even if XML (or another worker) is using _Head tags.
        public static void WriteSharedProfileTo(ILoadReferenceable target, DecalProfile profile)
        {
            WriteProfileTo(target, profile, TagBody);
            WriteProfileTo(target, profile, TagHead);
        }

        public static bool IsHelmet(Apparel a)
        {
            if (a?.def?.apparel == null) return false;

            // Layer check (strong signal)
            if (a.def.apparel.layers != null && a.def.apparel.layers.Any(l => l != null && l.defName == "Overhead"))
                return true;

            // Body part group check. Do not reference BodyPartGroupDefOf.UpperFace (not in your version).
            if (a.def.apparel.bodyPartGroups != null)
            {
                foreach (var g in a.def.apparel.bodyPartGroups)
                {
                    if (g == null) continue;

                    if (g == BodyPartGroupDefOf.FullHead) return true;
                    if (g == BodyPartGroupDefOf.UpperHead) return true;
                    if (g == BodyPartGroupDefOf.Eyes) return true;

                    // Compatibility with defs that use this group name
                    if (g.defName == "UpperFace") return true;
                }
            }

            return false;
        }

        // ------------------------------------------------------------
        // Read / write tagged data (VEF tagged props)
        // ------------------------------------------------------------

        // Backward compatible: body tags by default
        public static DecalProfile ReadProfileFrom(ILoadReferenceable target)
            => ReadProfileFrom(target, TagBody);

        public static DecalProfile ReadProfileFrom(ILoadReferenceable target, DecalTagSet tags)
        {
            bool active = target.GetStringByTag(tags.Active) != null;

            var sp = target.GetStringByTag(tags.SymbolPath);
            var sc = target.GetColorByTag(tags.SymbolColor);

            return new DecalProfile
            {
                Active = active,
                SymbolPath = sp?.value ?? "",
                SymbolColor = sc?.value ?? new Color(0.6f, 0.6f, 0.6f, 1f),
            };
        }

        // Backward compatible: body tags by default
        public static void WriteProfileTo(ILoadReferenceable target, DecalProfile profile)
            => WriteProfileTo(target, profile, TagBody);

        public static void WriteProfileTo(ILoadReferenceable target, DecalProfile profile, DecalTagSet tags)
        {
            if (profile.Active)
            {
                // IMPORTANT: use "true" not "1" so tagRequirements logic that expects truthy strings works.
                TaggedTextPropManager.SetTagItem(target, new TaggedText(tags.Active, "true"));
                TaggedTextPropManager.SetTagItem(target, new TaggedText(tags.SymbolPath, profile.SymbolPath ?? ""));
                TaggedColorPropManager.SetTagItem(target, new TaggedColor(tags.SymbolColor, profile.SymbolColor));
            }
            else
            {
                // Remove tags so the XML tagRequirements fails and fallback texPaths are used
                target.RemoveStringTag(tags.Active);
                target.RemoveStringTag(tags.SymbolPath);
                target.RemoveColorTag(tags.SymbolColor);
            }
        }

        // Existing behavior (body tags only)
        public static void ApplyProfileToPawnAndDecalApparel(Pawn pawn, DecalProfile profile)
        {
            // Shared by default: write both tag sets so either XML scheme works.
            WriteSharedProfileTo(pawn, profile);

            // Mirror only to apparel explicitly marked as decal-supporting.
            // If you want helmets to be editable too, add CompEditDecalMarker to those helmet ThingDefs.
            foreach (var a in PawnDecalApparelAndHelmets(pawn))
            {
                if (a == null) continue;
                WriteSharedProfileTo(a, profile);
            }

            DirtyPawnGraphics(pawn);
        }

        // Convenience alias: this is what the dialog should call.
        // It persists the choice on the pawn and mirrors to eligible worn apparel.
        public static void ApplyProfileToPawnAndWornApparel(Pawn pawn, DecalProfile profile)
        {
            ApplyProfileToPawnAndDecalApparel(pawn, profile);
        }

        // Debug helper: call this from anywhere to verify tags exist on pawn/apparel.
        public static void DumpDecalTagsToLog(Pawn pawn)
        {
            if (pawn == null) return;

            var pb = pawn.GetColorByTag(TagBody.SymbolColor)?.value;
            var ph = pawn.GetColorByTag(TagHead.SymbolColor)?.value;
            Log.Message($"[BNF.Decals] Pawn tags: ActiveBody={(pawn.GetStringByTag(TagBody.Active) != null)} ActiveHead={(pawn.GetStringByTag(TagHead.Active) != null)} " +
                        $"ColorBody={(pb.HasValue ? pb.Value.ToString() : "null")} ColorHead={(ph.HasValue ? ph.Value.ToString() : "null")} " +
                        $"SymbolBody={(pawn.GetStringByTag(TagBody.SymbolPath)?.value ?? "null")} SymbolHead={(pawn.GetStringByTag(TagHead.SymbolPath)?.value ?? "null")}");

            if (pawn.apparel?.WornApparel == null) return;

            foreach (var a in pawn.apparel.WornApparel)
            {
                if (a == null) continue;
                var ab = a.GetColorByTag(TagBody.SymbolColor)?.value;
                var ah = a.GetColorByTag(TagHead.SymbolColor)?.value;
                Log.Message($"[BNF.Decals] Apparel {a.LabelCap}: ActiveBody={(a.GetStringByTag(TagBody.Active) != null)} ActiveHead={(a.GetStringByTag(TagHead.Active) != null)} " +
                            $"ColorBody={(ab.HasValue ? ab.Value.ToString() : "null")} ColorHead={(ah.HasValue ? ah.Value.ToString() : "null")} " +
                            $"SymbolBody={(a.GetStringByTag(TagBody.SymbolPath)?.value ?? "null")} SymbolHead={(a.GetStringByTag(TagHead.SymbolPath)?.value ?? "null")}");
            }
        }

        // New behavior: two independent systems
        public static void ApplyProfilesToPawnAndDecalApparel(
            Pawn pawn,
            DecalProfile bodyProfile,
            DecalProfile headProfile,
            bool link)
        {
            if (pawn == null) return;

            if (link)
            {
                // Write the same profile to both tag sets
                WriteProfileTo(pawn, bodyProfile, TagBody);
                WriteProfileTo(pawn, bodyProfile, TagHead);

                foreach (var a in PawnDecalApparelAndHelmets(pawn))
                {
                    WriteProfileTo(a, bodyProfile, TagBody);
                    WriteProfileTo(a, bodyProfile, TagHead);
                }
            }
            else
            {
                // Body tags go to pawn and non-helmet apparel
                WriteProfileTo(pawn, bodyProfile, TagBody);
                // Head tags go to pawn and helmet apparel
                WriteProfileTo(pawn, headProfile, TagHead);

                foreach (var a in PawnDecalApparelAndHelmets(pawn))
                {
                    if (IsHelmet(a))
                    {
                        WriteProfileTo(a, headProfile, TagHead);
                        // Optional: clear body tags from helmets so nothing accidental reads them
                        // WriteProfileTo(a, DecalProfile.Default, TagBody);
                    }
                    else
                    {
                        WriteProfileTo(a, bodyProfile, TagBody);
                        // Optional: clear head tags from armor
                        // WriteProfileTo(a, DecalProfile.Default, TagHead);
                    }
                }
            }

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
