using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace BNF.Decals
{
    [StaticConstructorOnStartup]
    public static class DecalBootstrap
    {
        static DecalBootstrap()
        {
            new Harmony("BNF.Decals").PatchAll();
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Patch_Pawn_GetGizmos_Decals
    {
        public static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            Pawn pawn = __instance;
            if (pawn == null) return;
            if (pawn.Faction != Faction.OfPlayerSilentFail) return;
            if (!DecalUtil.IsHumanlikePawn(pawn)) return;

            // Original gating only showed the gizmo if the pawn had at least one marker-comp decal apparel.
            // Helmets typically won't have that comp, but you still want to set a shared pawn decal that
            // can be mirrored onto helmets.
            bool hasDecalApparel = DecalUtil.PawnHasAnyDecalApparel(pawn);
            bool hasHelmet = pawn.apparel?.WornApparel != null && pawn.apparel.WornApparel.Any(IsHelmet);

            if (!hasDecalApparel) return;

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

        private static bool IsHelmet(Apparel a)
        {
            if (a?.def?.apparel == null) return false;

            // Most helmets are in the Overhead layer.
            if (a.def.apparel.layers != null && a.def.apparel.layers.Any(l => l != null && l.defName == "Overhead"))
                return true;

            // Some headgear doesn't use Overhead but still covers head groups.
            if (a.def.apparel.bodyPartGroups != null)
            {
                foreach (var g in a.def.apparel.bodyPartGroups)
                {
                    if (g == null) continue;

                    if (g == BodyPartGroupDefOf.FullHead) return true;
                    if (g == BodyPartGroupDefOf.UpperHead) return true;
                    if (g == BodyPartGroupDefOf.Eyes) return true;

                    // Do NOT reference BodyPartGroupDefOf.UpperFace (not in Core). Use name check.
                    if (g.defName == "UpperFace") return true;
                }
            }

            return false;
        }
    }

    [StaticConstructorOnStartup]
    internal static class DecalTex
    {
        internal static readonly Texture2D Checker;

        static DecalTex()
        {
            Checker = BuildChecker(16);
        }

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

        private int selIndex;
        private DecalSymbolDef? selDef;

        private string rStr = "";
        private string gStr = "";
        private string bStr = "";
        private string hexStr = "";
        private bool rgbDirty;
        private bool hexDirty;

        private static readonly List<Color> customColors = new List<Color>(24);

        private const float HeaderH = 34f;
        private const float ApplyH = 54f;

        private const float Pad = 10f;
        private const float Gap = 6f;
        private const float Tight = 3f;

        private const float CyclerH = 30f;

        private const float BottomH = 305f;
        private const float SectionHeaderH = 24f;

        private const int SavedCols = 12;
        private const int SavedRows = 2;
        private const float CellGap = 2f;

        public override Vector2 InitialSize => new Vector2(760f, 640f);

        public Dialog_EditDecals(Pawn pawn)
        {
            this.pawn = pawn;

            forcePause = false;
            doCloseX = false;
            closeOnAccept = false;
            closeOnCancel = false;
            absorbInputAroundWindow = true;

            profile = DecalUtil.ReadProfileFrom(pawn);
            original = CopyProfile(profile);

            symbols = DecalUtil.AllSymbols() ?? new List<DecalSymbolDef>();
            palette = DecalUtil.FixedPalette_LargeSpread() ?? new List<Color>();

            selIndex = 0;
            if (!profile.SymbolPath.NullOrEmpty() && symbols.Count > 0)
            {
                int idx = symbols.FindIndex(d => d != null && d.path == profile.SymbolPath);
                if (idx >= 0) selIndex = idx;
            }

            SyncSelectionFromIndex();
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
            float bottom = Mathf.Clamp(
                Mathf.Min(BottomH, rect.height * 0.55f),
                240f,
                360f
            );

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
                selIndex = (selIndex - 1 + symbols.Count) % symbols.Count;
                SyncSelectionFromIndex();
            }

            if (Widgets.ButtonText(right, ">") && hasAny)
            {
                selIndex = (selIndex + 1) % symbols.Count;
                SyncSelectionFromIndex();
            }

            GUI.color = Color.white;

            if (Widgets.ButtonText(random, "BNF_Decals_RandomSymbol".Translate()) && hasAny)
            {
                selIndex = Rand.Range(0, symbols.Count);
                SyncSelectionFromIndex();
            }

            if (Mouse.IsOver(label))
                Widgets.DrawHighlight(label);

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(label, selDef != null ? selDef.LabelCap : "BNF_Decals_NoSymbolsFound".Translate());
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

            float leftW = Mathf.Floor(rect.width * 0.50f);
            Rect left = new Rect(rect.x, rect.y, leftW, rect.height);
            Rect right = new Rect(left.xMax + Gap, rect.y, rect.width - leftW - Gap, rect.height);

            DrawPaletteAndCustom(left);
            DrawInputsAndButtons(right);
        }

        private void DrawPaletteAndCustom(Rect rect)
        {
            float palH = Mathf.Floor(rect.height * 0.50f);
            float savedH = rect.height - palH - Tight;
            if (savedH < 86f)
            {
                savedH = 86f;
                palH = rect.height - savedH - Tight;
            }

            Rect palRect = new Rect(rect.x, rect.y, rect.width, palH);
            Rect savedRect = new Rect(rect.x, palRect.yMax + Tight, rect.width, savedH);

            float cellHint = DrawPalette(palRect);
            DrawCustomColors(savedRect, cellHint);
        }

        private float DrawPalette(Rect rect)
        {
            DrawSectionHeader(new Rect(rect.x, rect.y, rect.width, SectionHeaderH), "BNF_Decals_Palette".Translate());

            Rect body = new Rect(rect.x, rect.y + SectionHeaderH + Tight, rect.width, rect.height - SectionHeaderH - Tight);

            float cellW = (body.width - (SavedCols - 1) * CellGap) / SavedCols;
            float cellH = (body.height - (SavedRows - 1) * CellGap) / SavedRows;
            float cell = Mathf.Clamp(Mathf.Floor(Mathf.Min(cellW, cellH)), 12f, 22f);

            int rows = 5;
            int cols = Mathf.Max(1, Mathf.CeilToInt(palette.Count / (float)rows));

            float gridW = cols * cell + (cols - 1) * CellGap;
            float gridH = rows * cell + (rows - 1) * CellGap;

            float startX = body.x + Mathf.Max(0f, (body.width - gridW) * 0.5f);
            float startY = body.y + Mathf.Max(0f, (body.height - gridH) * 0.5f);

            for (int i = 0; i < palette.Count; i++)
            {
                int r = i % rows;
                int c = i / rows;

                Rect cr = new Rect(startX + c * (cell + CellGap), startY + r * (cell + CellGap), cell, cell);
                if (cr.xMax > body.xMax + 0.1f || cr.yMax > body.yMax + 0.1f) continue;

                Color col = palette[i];

                Widgets.DrawBoxSolid(cr, col);
                Widgets.DrawBox(cr);

                if (profile.Active && profile.SymbolColor.IndistinguishableFrom(col))
                    Widgets.DrawBoxSolid(cr.ContractedBy(3f), new Color(1f, 1f, 1f, 0.18f));

                if (Widgets.ButtonInvisible(cr))
                {
                    SetColor(col);
                    SyncInputsFromColor();
                }

                if (Mouse.IsOver(cr))
                    TooltipHandler.TipRegion(cr, PrettyHex(col));
            }

            return cell;
        }

        private void DrawCustomColors(Rect rect, float cellHint)
        {
            Rect header = new Rect(rect.x, rect.y, rect.width, SectionHeaderH);

            float btnW = Mathf.Min(170f, header.width * 0.55f);
            Rect title = new Rect(header.x, header.y, header.width - btnW - 4f, header.height);
            Rect reset = new Rect(title.xMax + 4f, header.y, btnW, header.height);

            DrawSectionHeader(title, "BNF_Decals_SavedColors".Translate());

            if (Widgets.ButtonText(reset, "BNF_Decals_ResetCustomColors".Translate()))
                customColors.Clear();

            Rect body = new Rect(rect.x, header.yMax + Tight, rect.width, rect.height - SectionHeaderH - Tight);

            float cellW = (body.width - (SavedCols - 1) * CellGap) / SavedCols;
            float cellH = (body.height - (SavedRows - 1) * CellGap) / SavedRows;
            float cell = Mathf.Clamp(cellHint, 12f, Mathf.Clamp(Mathf.Floor(Mathf.Min(cellW, cellH)), 12f, 22f));

            float gridW = SavedCols * cell + (SavedCols - 1) * CellGap;
            float gridH = SavedRows * cell + (SavedRows - 1) * CellGap;

            float startX = body.x + Mathf.Max(0f, (body.width - gridW) * 0.5f);
            float startY = body.y + Mathf.Max(0f, (body.height - gridH) * 0.5f);

            int cap = SavedCols * SavedRows;

            for (int i = 0; i < cap; i++)
            {
                int rr = i / SavedCols;
                int cc = i % SavedCols;

                Rect cr = new Rect(startX + cc * (cell + CellGap), startY + rr * (cell + CellGap), cell, cell);

                Widgets.DrawBoxSolid(cr, new Color(0.10f, 0.10f, 0.10f, 1f));
                Widgets.DrawBox(cr);

                if (i >= customColors.Count) continue;

                Color col = customColors[i];

                Widgets.DrawBoxSolid(cr.ContractedBy(2f), col);

                if (profile.Active && profile.SymbolColor.IndistinguishableFrom(col))
                    Widgets.DrawBoxSolid(cr.ContractedBy(5f), new Color(1f, 1f, 1f, 0.18f));

                if (Widgets.ButtonInvisible(cr))
                {
                    SetColor(col);
                    SyncInputsFromColor();
                }

                if (Mouse.IsOver(cr))
                {
                    TooltipHandler.TipRegion(cr, PrettyHex(col) + "\n" + "BNF_Decals_RightClickRemoves".Translate());

                    Event ev = Event.current;
                    if (ev != null && ev.type == EventType.MouseDown && ev.button == 1)
                    {
                        customColors.RemoveAt(i);
                        ev.Use();
                    }
                }
            }
        }

        private void DrawInputsAndButtons(Rect rect)
        {
            DrawSectionHeader(new Rect(rect.x, rect.y, rect.width, SectionHeaderH), "BNF_Decals_Color".Translate());

            Rect body = new Rect(rect.x, rect.y + SectionHeaderH + Tight, rect.width, rect.height - SectionHeaderH - Tight);

            float innerGap = 6f;
            float btnColW = Mathf.Clamp(body.width * 0.52f, 190f, 265f);
            float inpW = body.width - btnColW - innerGap;

            Rect inputs = new Rect(body.x, body.y, inpW, body.height);
            Rect buttons = new Rect(inputs.xMax + innerGap, body.y, btnColW, body.height);

            float labelH = 18f;
            float buttonsTopY = inputs.y + labelH + Tight;

            DrawInputs(inputs);
            DrawButtons(buttons, buttonsTopY);

            if (!rgbDirty && !hexDirty)
                SyncInputsFromColor();
        }

        private void DrawInputs(Rect rect)
        {
            float y = rect.y;

            float labelH = 18f;
            float rowH = 22f;
            float rowGap = 2f;

            Widgets.Label(new Rect(rect.x, y, rect.width, labelH), "BNF_Decals_SelectedColor".Translate());
            y += labelH + Tight;

            Rect swatch = new Rect(rect.x, y, rect.width, 18f);
            Widgets.DrawBoxSolid(swatch, profile.SymbolColor);
            Widgets.DrawBox(swatch);
            TooltipHandler.TipRegion(swatch, PrettyHex(profile.SymbolColor));
            y = swatch.yMax + Tight;

            Widgets.Label(new Rect(rect.x, y, rect.width, labelH), "BNF_Decals_RGB".Translate());
            y += labelH + Tight;

            bool chR = DrawRGBRow(rect.x, y, rowH, "R", ref rStr); y += rowH + rowGap;
            bool chG = DrawRGBRow(rect.x, y, rowH, "G", ref gStr); y += rowH + rowGap;
            bool chB = DrawRGBRow(rect.x, y, rowH, "B", ref bStr); y += rowH + Tight;

            if (chR || chG || chB) rgbDirty = true;

            if (rgbDirty && TryParseRGB(rStr, gStr, bStr, out Color rgb))
            {
                SetColor(rgb);
                hexStr = PrettyHex(profile.SymbolColor);
                hexDirty = false;
            }

            Widgets.Label(new Rect(rect.x, y, rect.width, labelH), "BNF_Decals_HEX".Translate());
            y += labelH + Tight;

            Rect hash = new Rect(rect.x, y + 2f, 16f, rowH);
            Widgets.Label(hash, "#");

            Rect field = new Rect(rect.x + 16f + 4f, y, 86f, rowH);
            string before = hexStr ?? "";
            hexStr = Widgets.TextField(field, before);
            if (hexStr != before) hexDirty = true;

            TooltipHandler.TipRegion(field, "RRGGBB");

            if (hexDirty && TryParseHex(hexStr, out Color hc))
            {
                SetColor(hc);

                ToRGBInt(profile.SymbolColor, out int rr, out int gg, out int bb);
                rStr = rr.ToString();
                gStr = gg.ToString();
                bStr = bb.ToString();
                rgbDirty = false;
            }
        }

        private bool DrawRGBRow(float x, float y, float rowH, string label, ref string value)
        {
            Rect l = new Rect(x, y + 2f, 18f, rowH);
            Widgets.Label(l, label);

            Rect f = new Rect(x + 18f + 4f, y, 46f, rowH);
            string before = value ?? "";
            value = Widgets.TextField(f, before);
            TooltipHandler.TipRegion(f, "0-255");

            return value != before;
        }

        private void DrawButtons(Rect rect, float topY)
        {
            GameFont old = Text.Font;
            Text.Font = GameFont.Small;

            const float btnH = 30f;
            const float btnGap = 5f;

            float btnW = Mathf.Clamp(rect.width * 0.78f, 160f, rect.width);
            float x = rect.xMax - btnW;

            float totalH = (btnH * 4f) + (btnGap * 3f);
            float y = Mathf.Clamp(topY, rect.y, rect.yMax - totalH);

            Rect save = new Rect(x, y, btnW, btnH); y = save.yMax + btnGap;
            Rect ideo = new Rect(x, y, btnW, btnH); y = ideo.yMax + btnGap;
            Rect fav = new Rect(x, y, btnW, btnH); y = fav.yMax + btnGap;
            Rect rnd = new Rect(x, y, btnW, btnH);

            if (Widgets.ButtonText(save, "BNF_Decals_SaveColor".Translate()))
                AddCustom(profile.SymbolColor);

            if (TryGetIdeoColor(pawn, out Color ideoColor))
            {
                if (Widgets.ButtonText(ideo, "BNF_Decals_SetIdeoColor".Translate()))
                {
                    SetColor(ideoColor);
                    SyncInputsFromColor();
                }
            }
            else
            {
                GUI.color = new Color(1f, 1f, 1f, 0.45f);
                Widgets.ButtonText(ideo, "BNF_Decals_SetIdeoColor".Translate());
                GUI.color = Color.white;
            }

            if (TryGetFavoriteColor(pawn, out Color favColor))
            {
                if (Widgets.ButtonText(fav, "BNF_Decals_SetFavoriteColor".Translate()))
                {
                    SetColor(favColor);
                    SyncInputsFromColor();
                }
            }
            else
            {
                GUI.color = new Color(1f, 1f, 1f, 0.45f);
                Widgets.ButtonText(fav, "BNF_Decals_SetFavoriteColor".Translate());
                GUI.color = Color.white;
            }

            if (Widgets.ButtonText(rnd, "BNF_Decals_RandomColor".Translate()))
            {
                SetColor(RandomNiceColor());
                SyncInputsFromColor();
            }

            Text.Font = old;
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
                ApplySharedProfileToPawnAndWornApparel();
                Close(doCloseSound: true);
            }

            if (Widgets.ButtonText(reset, "BNF_Decals_Reset".Translate()))
            {
                profile = CopyProfile(original);

                selIndex = 0;
                if (!profile.SymbolPath.NullOrEmpty() && symbols.Count > 0)
                {
                    int idx = symbols.FindIndex(d => d != null && d.path == profile.SymbolPath);
                    if (idx >= 0) selIndex = idx;
                }

                SyncSelectionFromIndex();
                SyncInputsFromColor();
            }
        }

        private void ApplySharedProfileToPawnAndWornApparel()
        {
            if (pawn == null) return;

            // Persist on pawn
            DecalUtil.WriteSharedProfileTo(pawn, profile);

            // Mirror to all worn apparel
            if (pawn.apparel?.WornApparel != null)
            {
                foreach (var a in pawn.apparel.WornApparel)
                {
                    if (a == null) continue;
                    DecalUtil.WriteSharedProfileTo(a, profile);
                }
            }

            DecalUtil.DirtyPawnGraphics(pawn);
        }

        private void DrawSectionHeader(Rect rect, string label)
        {
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(rect, label);
            Text.Anchor = TextAnchor.UpperLeft;

            Color old = GUI.color;
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

        private void SyncSelectionFromIndex()
        {
            if (symbols.Count == 0)
            {
                selDef = null;
                profile.SymbolPath = "";
                profile.Active = false;
                return;
            }

            if (selIndex < 0) selIndex = 0;
            if (selIndex >= symbols.Count) selIndex = symbols.Count - 1;

            selDef = symbols[selIndex];

            if (selDef == null)
            {
                for (int i = 0; i < symbols.Count; i++)
                {
                    int idx = (selIndex + i) % symbols.Count;
                    if (symbols[idx] != null)
                    {
                        selIndex = idx;
                        selDef = symbols[idx];
                        break;
                    }
                }
            }

            if (selDef != null)
            {
                profile.SymbolPath = selDef.path ?? "";
                profile.Active = !selDef.blankType;
            }
            else
            {
                profile.SymbolPath = "";
                profile.Active = false;
            }
        }

        private string GetDescription()
        {
            if (selDef == null) return "BNF_Decals_NoSelectionDesc".Translate();
            if (!selDef.description.NullOrEmpty()) return selDef.description;
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
            hexStr = PrettyHex(profile.SymbolColor);

            rgbDirty = false;
            hexDirty = false;
        }

        private static Texture2D GetSymbolTex(string path)
        {
            if (path.NullOrEmpty()) return null;
            return ContentFinder<Texture2D>.Get(path, false);
        }

        private static void AddCustom(Color c)
        {
            for (int i = 0; i < customColors.Count; i++)
                if (customColors[i].IndistinguishableFrom(c))
                    return;

            customColors.Add(c);
            if (customColors.Count > 24)
                customColors.RemoveAt(0);
        }

        private static DecalProfile CopyProfile(DecalProfile src)
        {
            DecalProfile p = default;
            p.SymbolPath = src.SymbolPath;
            p.Active = src.Active;
            p.SymbolColor = src.SymbolColor;
            return p;
        }

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
            if (v < 0) v = 0;
            if (v > 255) v = 255;
            return true;
        }

        private static bool TryParseHex(string input, out Color c)
        {
            c = Color.white;
            if (input.NullOrEmpty()) return false;

            string s = input.Trim();
            if (s.StartsWith("#")) s = s.Substring(1);
            if (s.Length != 6) return false;

            if (!TryHexByte(s, 0, out byte r)) return false;
            if (!TryHexByte(s, 2, out byte g)) return false;
            if (!TryHexByte(s, 4, out byte b)) return false;

            c = new Color(r / 255f, g / 255f, b / 255f, 1f);
            return true;
        }

        private static bool TryHexByte(string s, int i, out byte v)
        {
            v = 0;
            int hi = HexVal(s[i]);
            int lo = HexVal(s[i + 1]);
            if (hi < 0 || lo < 0) return false;
            v = (byte)((hi << 4) | lo);
            return true;
        }

        private static int HexVal(char ch)
        {
            if (ch >= '0' && ch <= '9') return ch - '0';
            if (ch >= 'a' && ch <= 'f') return 10 + (ch - 'a');
            if (ch >= 'A' && ch <= 'F') return 10 + (ch - 'A');
            return -1;
        }

        private static void ToRGBInt(Color c, out int r, out int g, out int b)
        {
            r = Mathf.RoundToInt(c.r * 255f);
            g = Mathf.RoundToInt(c.g * 255f);
            b = Mathf.RoundToInt(c.b * 255f);

            if (r < 0) r = 0;
            if (r > 255) r = 255;

            if (g < 0) g = 0;
            if (g > 255) g = 255;

            if (b < 0) b = 0;
            if (b > 255) b = 255;
        }

        private static string PrettyHex(Color c)
        {
            ToRGBInt(c, out int r, out int g, out int b);
            return $"#{r:X2}{g:X2}{b:X2}";
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
