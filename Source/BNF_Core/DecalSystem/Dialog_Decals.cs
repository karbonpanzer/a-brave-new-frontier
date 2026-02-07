using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace BNF.Decals
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
            catch (Exception e)
            {
                Log.Error("[BNF] Decal System failed to load:\n" + e);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Patch_Pawn_GetGizmos_Decals
    {
        public static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            var pawn = __instance;
            if (pawn == null) return;
            if (pawn.Faction != Faction.OfPlayerSilentFail) return;
            if (!DecalUtil.IsHumanlikePawn(pawn)) return;
            if (!DecalUtil.PawnHasAnyDecalApparel(pawn)) return;

            var cmd = new Command_Action
            {
                defaultLabel = "BNF_StyleDecalsGizmo".Translate(pawn.LabelCap),
                defaultDesc = "BNF_StyleDecalsDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/CustomizeDecal", false)
                       ?? ContentFinder<Texture2D>.Get("UI/Commands/Rename", true),
                action = () => Find.WindowStack.Add(new Dialog_EditDecals(pawn))
            };

            __result = __result.Concat(new Gizmo[] { cmd });
        }
    }

    [StaticConstructorOnStartup]
    internal static class DecalTex
    {
        internal static readonly Texture2D Checker;

        static DecalTex() => Checker = BuildChecker(16);

        private static Texture2D BuildChecker(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point
            };

            var px = new Color32[size * size];
            Color32 a = new Color32(28, 28, 28, 255);
            Color32 b = new Color32(40, 40, 40, 255);

            int half = Mathf.Max(1, size / 2);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool even = ((x / half) + (y / half)) % 2 == 0;
                    px[y * size + x] = even ? a : b;
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }
    }

    public sealed class Dialog_EditDecals : Window
    {
        private readonly Pawn pawn;

        private DecalProfile profile;
        private readonly DecalProfile original;

        private readonly List<DecalSymbolDef> symbols;
        private readonly List<Color> palette;

        private int selectedIndex;
        private DecalSymbolDef selectedSymbol;

        private string rStr = "";
        private string gStr = "";
        private string bStr = "";
        private string hexStr = "";
        private bool rgbDirty;
        private bool hexDirty;

        private const float HeaderH = 34f;
        private const float ApplyH = 54f;

        private const float Pad = 10f;

        // tighter spacing so left column gets more usable width
        private const float Gap = 3f;
        private const float Tight = 2f;

        private const float CyclerH = 30f;

        private const float BottomH = 270f;
        private const float SectionHeaderH = 24f;

        public override Vector2 InitialSize => new Vector2(760f, 580f);

        public Dialog_EditDecals(Pawn pawn)
        {
            this.pawn = pawn;

            forcePause = false;
            doCloseX = false;
            closeOnAccept = false;
            closeOnCancel = false;
            absorbInputAroundWindow = true;

            profile = DecalUtil.ReadProfileFrom(pawn);
            original = profile;

            symbols = DecalUtil.AllSymbols() ?? new List<DecalSymbolDef>();
            palette = DecalUtil.FixedPalette_LargeSpread() ?? new List<Color>();

            selectedIndex = FindSymbolIndex(profile.SymbolPath);
            SyncSelection();
            SyncInputsFromColor();

            _ = DecalTex.Checker;
        }

        public override void DoWindowContents(Rect inRect)
        {
            DrawHeader(new Rect(inRect.x, inRect.y, inRect.width, HeaderH));

            Rect apply = new Rect(inRect.x, inRect.yMax - ApplyH, inRect.width, ApplyH);
            Rect main = new Rect(inRect.x, inRect.y + HeaderH, inRect.width, inRect.height - HeaderH - ApplyH);

            DrawMain(main);
            DrawApply(apply);
        }

        private void DrawHeader(Rect rect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(rect, "BNF_StyleDecalsTitle".Translate(pawn.Name.ToStringShort));
            Text.Font = GameFont.Small;
        }

        private void DrawMain(Rect rect)
        {
            float bottom = Mathf.Clamp(Mathf.Min(BottomH, rect.height * 0.55f), 240f, 360f);

            Rect top = new Rect(rect.x, rect.y, rect.width, rect.height - bottom - Gap);
            Rect bot = new Rect(rect.x, top.yMax + Gap, rect.width, rect.yMax - (top.yMax + Gap));

            DrawTop(top);
            DrawBottom(bot);
        }

        private void DrawTop(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect inner = rect.ContractedBy(Pad);

            Rect cycler = new Rect(inner.x, inner.y, inner.width, CyclerH);
            DrawCycler(cycler);

            Rect content = new Rect(inner.x, cycler.yMax + Tight, inner.width, inner.yMax - (cycler.yMax + Tight));

            float leftW = Mathf.Floor(content.width * 0.46f);
            Rect left = new Rect(content.x, content.y, leftW, content.height);
            Rect right = new Rect(left.xMax + Gap, content.y, content.width - leftW - Gap, content.height);

            DrawPreview(left);
            DrawDescription(right);
        }

        private void DrawCycler(Rect rect)
        {
            bool hasAny = symbols.Count > 0;

            float arrowW = 34f;
            float randomW = 150f;

            Rect left = new Rect(rect.x, rect.y, arrowW, rect.height);
            Rect random = new Rect(rect.xMax - randomW, rect.y, randomW, rect.height);
            Rect right = new Rect(random.x - Tight - arrowW, rect.y, arrowW, rect.height);
            Rect label = new Rect(left.xMax + Tight, rect.y, right.x - (left.xMax + Tight) - Tight, rect.height);

            GUI.color = hasAny ? Color.white : new Color(1f, 1f, 1f, 0.45f);

            if (Widgets.ButtonText(left, "<") && hasAny)
            {
                selectedIndex = (selectedIndex - 1 + symbols.Count) % symbols.Count;
                SyncSelection();
            }

            if (Widgets.ButtonText(right, ">") && hasAny)
            {
                selectedIndex = (selectedIndex + 1) % symbols.Count;
                SyncSelection();
            }

            GUI.color = Color.white;

            if (Widgets.ButtonText(random, "BNF_Decals_RandomSymbol".Translate()) && hasAny)
            {
                selectedIndex = Rand.Range(0, symbols.Count);
                SyncSelection();
            }

            if (Mouse.IsOver(label))
                Widgets.DrawHighlight(label);

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(label, selectedSymbol != null ? selectedSymbol.LabelCap : "BNF_Decals_NoSymbolsFound".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawPreview(Rect rect)
        {
            DrawSectionHeader(new Rect(rect.x, rect.y, rect.width, SectionHeaderH), "BNF_Decals_Preview".Translate());

            Rect body = new Rect(rect.x, rect.y + SectionHeaderH + Tight, rect.width, rect.height - SectionHeaderH - Tight);

            float size = Mathf.Clamp(Mathf.Min(body.width, body.height), 120f, 200f);
            Rect box = new Rect(body.x + (body.width - size) * 0.5f, body.y + (body.height - size) * 0.5f, size, size);

            DrawChecker(box);
            Widgets.DrawBox(box);

            Texture2D tex = GetSymbolTex(profile.SymbolPath);
            if (tex != null)
            {
                GUI.color = profile.Active ? profile.SymbolColor : new Color(1f, 1f, 1f, 0.25f);
                GUI.DrawTexture(box.ContractedBy(size * 0.10f), tex);
                GUI.color = Color.white;
            }
        }

        private void DrawDescription(Rect rect)
        {
            DrawSectionHeader(new Rect(rect.x, rect.y, rect.width, SectionHeaderH), "BNF_Decals_Description".Translate());

            Rect box = new Rect(rect.x, rect.y + SectionHeaderH + Tight, rect.width, rect.height - SectionHeaderH - Tight);
            Widgets.DrawMenuSection(box);

            Rect inner = box.ContractedBy(8f);
            Widgets.DrawBoxSolid(inner, new Color(0f, 0f, 0f, 0.12f));

            Widgets.Label(inner.ContractedBy(6f), GetDescription());
        }

        private void DrawBottom(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            rect = rect.ContractedBy(7f);

            // Give more space to left column (palette/buttons), push inputs right.
            float leftW = Mathf.Floor(rect.width * 0.62f);
            Rect leftCol = new Rect(rect.x, rect.y, leftW, rect.height);
            Rect rightCol = new Rect(leftCol.xMax + Gap, rect.y, rect.width - leftW - Gap, rect.height);

            // buttons now take two rows (side-by-side row + random row)
            const float btnH = 30f;
            const float btnGapY = 4f;
            float buttonsH = (btnH * 2f) + btnGapY;

            float paletteH = Mathf.Max(140f, leftCol.height - buttonsH - Gap);
            Rect paletteRect = new Rect(leftCol.x, leftCol.y, leftCol.width, paletteH);
            Rect buttonsRect = new Rect(leftCol.x, paletteRect.yMax + Gap, leftCol.width, leftCol.yMax - (paletteRect.yMax + Gap));

            DrawPalette(paletteRect);
            DrawColorButtons(buttonsRect);

            DrawInputsPanel(rightCol);
        }

        private void DrawPalette(Rect rect)
        {
            DrawSectionHeader(new Rect(rect.x, rect.y, rect.width, SectionHeaderH), "BNF_Decals_Palette".Translate());

            Rect body = new Rect(rect.x, rect.y + SectionHeaderH + 2f, rect.width, rect.height - SectionHeaderH - 2f);

            const float cell = 24f;
            const float gap = 2f;

            // Dynamic rows and cols based on available space so it doesn't "cap" at 4 rows.
            int cols = Mathf.Max(1, Mathf.FloorToInt((body.width + gap) / (cell + gap)));
            int rows = Mathf.Max(1, Mathf.FloorToInt((body.height + gap) / (cell + gap)));

            int maxVisible = rows * cols;

            float gridW = cols * cell + (cols - 1) * gap;
            float gridH = rows * cell + (rows - 1) * gap;

            float startX = body.x + Mathf.Max(0f, (body.width - gridW) * 0.5f);
            float startY = body.y + Mathf.Max(0f, (body.height - gridH) * 0.5f);

            int count = Mathf.Min(palette.Count, maxVisible);

            for (int i = 0; i < count; i++)
            {
                int r = i % rows;
                int c = i / rows;
                if (c >= cols) break;

                Rect cr = new Rect(startX + c * (cell + gap), startY + r * (cell + gap), cell, cell);
                Color col = palette[i];

                Widgets.DrawBoxSolid(cr, col);
                Widgets.DrawBox(cr);

                if (profile.Active && profile.SymbolColor.IndistinguishableFrom(col))
                    Widgets.DrawBoxSolid(cr.ContractedBy(4f), new Color(1f, 1f, 1f, 0.18f));

                if (Widgets.ButtonInvisible(cr))
                {
                    SetColor(col);
                    SyncInputsFromColor();
                }

                if (Mouse.IsOver(cr))
                    TooltipHandler.TipRegion(cr, DecalUtil.ColorToHex(col));
            }
        }

        private void DrawColorButtons(Rect rect)
        {
            var old = Text.Font;
            Text.Font = GameFont.Small;

            const float btnH = 30f;
            const float gapX = 4f;
            const float gapY = 4f;

            float btnW = (rect.width - gapX) * 0.5f;

            // Center the two rows vertically within the available rect.
            float totalH = (btnH * 2f) + gapY;
            float y = rect.y + Mathf.Max(0f, (rect.height - totalH) * 0.5f);

            // Row 1: Ideo + Favorite side by side
            Rect left = new Rect(rect.x, y, btnW, btnH);
            Rect right = new Rect(left.xMax + gapX, y, btnW, btnH);

            // Row 2: Random full width
            Rect random = new Rect(rect.x, left.yMax + gapY, rect.width, btnH);

            if (TryGetIdeoColor(pawn, out Color ideoColor))
            {
                if (Widgets.ButtonText(left, "BNF_Decals_SetIdeoColor".Translate()))
                {
                    SetColor(ideoColor);
                    SyncInputsFromColor();
                }
            }
            else
            {
                GUI.color = new Color(1f, 1f, 1f, 0.45f);
                Widgets.ButtonText(left, "BNF_Decals_SetIdeoColor".Translate());
                GUI.color = Color.white;
            }

            if (TryGetFavoriteColor(pawn, out Color favColor))
            {
                if (Widgets.ButtonText(right, "BNF_Decals_SetFavoriteColor".Translate()))
                {
                    SetColor(favColor);
                    SyncInputsFromColor();
                }
            }
            else
            {
                GUI.color = new Color(1f, 1f, 1f, 0.45f);
                Widgets.ButtonText(right, "BNF_Decals_SetFavoriteColor".Translate());
                GUI.color = Color.white;
            }

            if (Widgets.ButtonText(random, "BNF_Decals_RandomColor".Translate()))
            {
                SetColor(RandomNiceColor());
                SyncInputsFromColor();
            }

            Text.Font = old;
        }

        private void DrawInputsPanel(Rect rect)
        {
            DrawSectionHeader(new Rect(rect.x, rect.y, rect.width, SectionHeaderH), "BNF_Decals_Color".Translate());

            Rect body = new Rect(rect.x, rect.y + SectionHeaderH + Tight, rect.width, rect.height - SectionHeaderH - Tight);

            DrawInputs(body);

            if (!rgbDirty && !hexDirty)
                SyncInputsFromColor();
        }

        private void DrawInputs(Rect rect)
        {
            // shift fields further to the right by using a left "label column"
            const float labelColW = 70f;

            float y = rect.y;

            const float labelH = 18f;
            const float rowH = 22f;
            const float rowGap = 2f;

            Widgets.Label(new Rect(rect.x, y, rect.width, labelH), "BNF_Decals_SelectedColor".Translate());
            y += labelH + Tight;

            Rect swatch = new Rect(rect.x, y, rect.width, 18f);
            Widgets.DrawBoxSolid(swatch, profile.SymbolColor);
            Widgets.DrawBox(swatch);
            TooltipHandler.TipRegion(swatch, DecalUtil.ColorToHex(profile.SymbolColor));
            y = swatch.yMax + Tight;

            Widgets.Label(new Rect(rect.x, y, rect.width, labelH), "BNF_Decals_RGB".Translate());
            y += labelH + Tight;

            bool chR = DrawRGBRow(rect.x, y, rect.width, rowH, labelColW, "R", ref rStr); y += rowH + rowGap;
            bool chG = DrawRGBRow(rect.x, y, rect.width, rowH, labelColW, "G", ref gStr); y += rowH + rowGap;
            bool chB = DrawRGBRow(rect.x, y, rect.width, rowH, labelColW, "B", ref bStr); y += rowH + Tight;

            if (chR || chG || chB) rgbDirty = true;

            if (rgbDirty && TryParseRGB(rStr, gStr, bStr, out Color rgb))
            {
                SetColor(rgb);
                hexStr = DecalUtil.ColorToHex(profile.SymbolColor);
                hexDirty = false;
            }

            Widgets.Label(new Rect(rect.x, y, rect.width, labelH), "BNF_Decals_HEX".Translate());
            y += labelH + Tight;

            Rect hash = new Rect(rect.x + labelColW - 16f, y + 2f, 16f, rowH);
            Widgets.Label(hash, "#");

            Rect field = new Rect(rect.x + labelColW, y, Mathf.Min(120f, rect.width - labelColW), rowH);
            string before = hexStr ?? "";
            hexStr = Widgets.TextField(field, before);
            if (hexStr != before) hexDirty = true;

            TooltipHandler.TipRegion(field, "RRGGBB");

            if (hexDirty && DecalUtil.TryParseHexColor(hexStr, out Color hc))
            {
                SetColor(hc);

                ToRGBInt(profile.SymbolColor, out int rr, out int gg, out int bb);
                rStr = rr.ToString();
                gStr = gg.ToString();
                bStr = bb.ToString();
                rgbDirty = false;
            }
        }

        private bool DrawRGBRow(float x, float y, float width, float rowH, float labelColW, string label, ref string value)
        {
            Widgets.Label(new Rect(x, y + 2f, labelColW, rowH), label);

            float fieldW = Mathf.Min(60f, width - labelColW);
            Rect f = new Rect(x + labelColW, y, fieldW, rowH);

            string before = value ?? "";
            value = Widgets.TextField(f, before);
            TooltipHandler.TipRegion(f, "0-255");

            return value != before;
        }

        private void DrawApply(Rect rect)
        {
            float btnW = 190f;
            float btnH = 34f;
            float midGap = 10f;

            float totalW = (btnW * 2f) + midGap;
            float startX = rect.x + (rect.width - totalW) * 0.5f;
            float y = rect.y + (rect.height - btnH) * 0.5f;

            Rect apply = new Rect(startX, y, btnW, btnH);
            Rect reset = new Rect(apply.xMax + midGap, y, btnW, btnH);

            if (Widgets.ButtonText(apply, "BNF_Decals_Apply".Translate()))
            {
                DecalUtil.ApplyAndRefresh(pawn, profile);
                Close(doCloseSound: true);
            }

            if (Widgets.ButtonText(reset, "BNF_Decals_Reset".Translate()))
            {
                profile = original;
                selectedIndex = FindSymbolIndex(profile.SymbolPath);
                SyncSelection();
                SyncInputsFromColor();
            }
        }

        private void DrawSectionHeader(Rect rect, string label)
        {
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(rect, label);
            Text.Anchor = TextAnchor.UpperLeft;

            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.12f);
            Widgets.DrawLineHorizontal(rect.x, rect.yMax - 1f, rect.width);
            GUI.color = old;
        }

        private void DrawChecker(Rect rect)
        {
            Texture2D tex = DecalTex.Checker;
            float tile = 16f;

            float u = rect.width / Mathf.Max(1f, tile);
            float v = rect.height / Mathf.Max(1f, tile);
            GUI.DrawTextureWithTexCoords(rect, tex, new Rect(0f, 0f, u, v));
        }

        private int FindSymbolIndex(string path)
        {
            if (path.NullOrEmpty() || symbols.Count == 0) return 0;

            int idx = symbols.FindIndex(d => d != null && d.path == path);
            return idx >= 0 ? idx : 0;
        }

        private void SyncSelection()
        {
            if (symbols.Count == 0)
            {
                selectedSymbol = null;
                profile.SymbolPath = "";
                profile.Active = false;
                return;
            }

            selectedIndex = Mathf.Clamp(selectedIndex, 0, symbols.Count - 1);
            selectedSymbol = symbols[selectedIndex];

            if (selectedSymbol == null)
            {
                for (int i = 0; i < symbols.Count; i++)
                {
                    int idx = (selectedIndex + i) % symbols.Count;
                    if (symbols[idx] != null)
                    {
                        selectedIndex = idx;
                        selectedSymbol = symbols[idx];
                        break;
                    }
                }
            }

            if (selectedSymbol != null)
            {
                profile.SymbolPath = selectedSymbol.path ?? "";
                profile.Active = !selectedSymbol.blankType;
            }
            else
            {
                profile.SymbolPath = "";
                profile.Active = false;
            }
        }

        private string GetDescription()
        {
            if (selectedSymbol == null) return "BNF_Decals_NoSelectionDesc".Translate();
            if (!selectedSymbol.description.NullOrEmpty()) return selectedSymbol.description;
            return "BNF_Decals_NoDesc".Translate();
        }

        private void SetColor(Color c)
        {
            profile.SymbolColor = c;
            if (!profile.Active) profile.Active = true;
        }

        private void SyncInputsFromColor()
        {
            ToRGBInt(profile.SymbolColor, out int rr, out int gg, out int bb);
            rStr = rr.ToString();
            gStr = gg.ToString();
            bStr = bb.ToString();
            hexStr = DecalUtil.ColorToHex(profile.SymbolColor);

            rgbDirty = false;
            hexDirty = false;
        }

        private static Texture2D GetSymbolTex(string path) =>
            path.NullOrEmpty() ? null : ContentFinder<Texture2D>.Get(path, false);

        private static bool TryGetIdeoColor(Pawn pawn, out Color c)
        {
            c = Color.white;
            if (pawn?.Ideo == null) return false;
            c = pawn.Ideo.Color;
            return true;
        }

        private static bool TryGetFavoriteColor(Pawn pawn, out Color c)
        {
            c = Color.white;
            ColorDef def = pawn?.story?.favoriteColor;
            if (def == null) return false;
            c = def.color;
            return true;
        }

        private static bool TryParseRGB(string rS, string gS, string bS, out Color c)
        {
            c = Color.white;

            if (!TryParseByte(rS, out int r)) return false;
            if (!TryParseByte(gS, out int g)) return false;
            if (!TryParseByte(bS, out int b)) return false;

            c = new Color(r / 255f, g / 255f, b / 255f, 1f);
            return true;
        }

        private static bool TryParseByte(string s, out int v)
        {
            v = 0;
            if (s.NullOrEmpty()) return false;
            if (!int.TryParse(s.Trim(), out v)) return false;
            v = Mathf.Clamp(v, 0, 255);
            return true;
        }

        private static void ToRGBInt(Color c, out int r, out int g, out int b)
        {
            r = Mathf.Clamp(Mathf.RoundToInt(c.r * 255f), 0, 255);
            g = Mathf.Clamp(Mathf.RoundToInt(c.g * 255f), 0, 255);
            b = Mathf.Clamp(Mathf.RoundToInt(c.b * 255f), 0, 255);
        }

        private static Color RandomNiceColor()
        {
            float h = Rand.Value;
            float s = Mathf.Lerp(0.55f, 1f, Rand.Value);
            float v = Mathf.Lerp(0.60f, 1f, Rand.Value);
            return Color.HSVToRGB(h, s, v);
        }
    }
}
