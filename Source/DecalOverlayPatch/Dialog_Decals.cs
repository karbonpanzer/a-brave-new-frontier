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
            new Harmony("BNF.Decals.CleanRoom").PatchAll();
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

            __result = __result.Concat(new Gizmo[]
            {
                new Command_Action
                {
                    defaultLabel = "BNF_StyleDecalsGizmo".Translate(pawn.LabelCap),
                    defaultDesc  = "BNF_StyleDecalsDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/CustomizeDecal", false)
                           ?? ContentFinder<Texture2D>.Get("UI/Commands/Rename", true),
                    action = () => Find.WindowStack.Add(new Dialog_EditDecals(pawn))
                }
            });
        }
    }

    public sealed class Dialog_EditDecals : Window
    {
        private readonly Pawn pawn;

        private DecalProfile profile;

        private readonly List<DecalSymbolDef> allSymbols;
        private readonly List<Color> paletteColors;

        private string search = "";
        private Vector2 symbolScroll;

        // Hex input
        private string hexInput = "#";
        private string hexError = null;

        // Palette grid
        private const int PaletteRows = 5;
        private const float PalCell = 20f;
        private const float PalGap = 4f;

        // Left column sizing
        private const float PreviewH = 280f;

        // Apply
        private const float ApplyBtnW = 200f;
        private const float ApplyBtnH = 35f;
        private const float ApplyStripH = 50f;

        public override Vector2 InitialSize => new Vector2(900f, 650f);

        public Dialog_EditDecals(Pawn pawn)
        {
            this.pawn = pawn;

            forcePause = false;
            doCloseX = false; // Apply is the close
            closeOnAccept = false;
            closeOnCancel = false;
            absorbInputAroundWindow = true;

            profile = DecalProfile.Default;

            allSymbols = DecalUtil.AllSymbols();
            paletteColors = DecalUtil.FixedPalette_LargeSpread();

            // Seed hex field from current color
            hexInput = DecalUtil.ColorToHex(profile.SymbolColor);
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f),
                "BNF_StyleDecalsTitle".Translate(pawn.Name.ToStringShort));
            Text.Font = GameFont.Small;

            Rect mainRect = new Rect(inRect.x, inRect.y + 38f, inRect.width, inRect.height - 38f - ApplyStripH);
            DrawMain(mainRect);

            Rect applyBtn = new Rect(
                inRect.x + (inRect.width - ApplyBtnW) * 0.5f,
                inRect.yMax - ApplyBtnH - 8f,
                ApplyBtnW,
                ApplyBtnH
            );

            if (Widgets.ButtonText(applyBtn, "Apply".Translate()))
            {
                DecalUtil.ApplyProfileToPawnAndDecalApparel(pawn, profile);
                Close(doCloseSound: true);
            }
        }

        private void DrawMain(Rect rect)
        {
            float gapH = 12f;
            float leftW = Mathf.Floor(rect.width * 0.44f);

            Rect leftRect = new Rect(rect.x, rect.y, leftW, rect.height);
            Rect rightRect = new Rect(leftRect.xMax + gapH, rect.y, rect.width - leftW - gapH, rect.height);

            Widgets.DrawMenuSection(leftRect);
            Widgets.DrawMenuSection(rightRect);

            leftRect = leftRect.ContractedBy(10f);
            rightRect = rightRect.ContractedBy(10f);

            DrawLeft(leftRect);
            DrawRight(rightRect);
        }

        // LEFT: preview top, hex input middle, palette bottom
        private void DrawLeft(Rect rect)
        {
            float gap = 10f;

            float previewH = Mathf.Min(PreviewH, rect.height * 0.52f);
            float hexH = 58f;
            float paletteH = rect.height - previewH - hexH - gap - gap;

            if (paletteH < 120f)
            {
                paletteH = 120f;
                hexH = Mathf.Max(40f, rect.height - previewH - paletteH - gap - gap);
            }

            Rect previewRect = new Rect(rect.x, rect.y, rect.width, previewH);
            Rect hexRect = new Rect(rect.x, previewRect.yMax + gap, rect.width, hexH);
            Rect paletteRect = new Rect(rect.x, hexRect.yMax + gap, rect.width, paletteH);

            DrawPreview(previewRect);
            DrawHexInput(hexRect);
            DrawPalette(paletteRect);
        }

        private void DrawPreview(Rect rect)
        {
            Rect header = new Rect(rect.x, rect.y, rect.width, 24f);
            Widgets.Label(header, "BNF_Decals_Preview".Translate());

            float iconSize = Mathf.Min(rect.width, rect.height - 34f);
            Rect iconBox = new Rect(rect.x + (rect.width - iconSize) / 2f, header.yMax + 8f, iconSize, iconSize);

            Widgets.DrawBoxSolid(iconBox, new Color(0.08f, 0.08f, 0.08f, 1f));
            Widgets.DrawBox(iconBox);

            Texture2D tex = null;
            if (!profile.SymbolPath.NullOrEmpty())
                tex = ContentFinder<Texture2D>.Get(profile.SymbolPath, false);

            if (tex != null && profile.Active)
            {
                GUI.color = profile.SymbolColor;
                GUI.DrawTexture(iconBox.ContractedBy(iconSize * 0.10f), tex);
                GUI.color = Color.white;
            }

            Rect info = new Rect(rect.x, iconBox.yMax + 6f, rect.width, rect.yMax - (iconBox.yMax + 6f));
            string label = profile.SymbolPath.NullOrEmpty() ? "BNF_NoSymbol".Translate() : profile.SymbolPath;
            Widgets.Label(info, label);
        }

        private void DrawHexInput(Rect rect)
        {
            Rect row = new Rect(rect.x, rect.y, rect.width, 28f);

            Rect labelRect = new Rect(row.x, row.y + 5f, 70f, 24f);
            Widgets.Label(labelRect, "Hex");

            Rect fieldRect = new Rect(row.x + 74f, row.y, row.width - 74f - 64f - 6f, 28f);
            hexInput = Widgets.TextField(fieldRect, hexInput ?? "");

            Rect setRect = new Rect(fieldRect.xMax + 6f, row.y, 64f, 28f);
            if (Widgets.ButtonText(setRect, "Set"))
                TryApplyHex(hexInput);

            // Optional error line
            if (!hexError.NullOrEmpty())
            {
                Rect errRect = new Rect(rect.x, row.yMax + 2f, rect.width, rect.height - (row.height + 2f));
                Color old = GUI.color;
                GUI.color = new Color(1f, 0.35f, 0.35f, 1f);
                Widgets.Label(errRect, hexError);
                GUI.color = old;
            }
        }

        private void TryApplyHex(string raw)
        {
            hexError = null;

            Color parsed;
            if (!DecalUtil.TryParseHexColor(raw, out parsed))
            {
                hexError = "Invalid hex";
                return;
            }

            profile.SymbolColor = parsed;
            if (!profile.Active) profile.Active = true;

            hexInput = DecalUtil.ColorToHex(parsed);
        }

        private void DrawPalette(Rect rect)
        {
            Rect header = new Rect(rect.x, rect.y, rect.width, 24f);
            Widgets.Label(header, "BNF_Decals_Palette".Translate());

            int rows = PaletteRows;
            int cols = Mathf.CeilToInt(paletteColors.Count / (float)rows);

            float gridW = cols * PalCell + (cols - 1) * PalGap;
            float gridH = rows * PalCell + (rows - 1) * PalGap;

            Rect usable = new Rect(rect.x, header.yMax + 8f, rect.width, rect.height - 32f);
            float startX = usable.x + Mathf.Max(0f, (usable.width - gridW) * 0.5f);
            float startY = usable.y + Mathf.Max(0f, (usable.height - gridH) * 0.5f);

            for (int idx = 0; idx < paletteColors.Count; idx++)
            {
                int r = idx % rows;
                int c = idx / rows;

                Rect cell = new Rect(
                    startX + c * (PalCell + PalGap),
                    startY + r * (PalCell + PalGap),
                    PalCell,
                    PalCell
                );

                Color col = paletteColors[idx];

                Widgets.DrawBoxSolid(cell, col);
                Widgets.DrawBox(cell);

                bool selected = profile.Active && profile.SymbolColor.IndistinguishableFrom(col);
                if (selected)
                    Widgets.DrawBoxSolid(cell.ContractedBy(4f), new Color(1f, 1f, 1f, 0.20f));

                if (Widgets.ButtonInvisible(cell))
                {
                    profile.SymbolColor = col;
                    if (!profile.Active) profile.Active = true;

                    hexInput = DecalUtil.ColorToHex(col);
                    hexError = null;
                }
            }
        }

        // RIGHT: decals + search
        private void DrawRight(Rect rect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 26f), "BNF_DecalSymbols".Translate());
            Text.Font = GameFont.Small;

            Rect searchRow = new Rect(rect.x, rect.y + 30f, rect.width, 28f);
            DrawSearchRow(searchRow);

            Rect gridRect = new Rect(rect.x, searchRow.yMax + 8f, rect.width, rect.yMax - (searchRow.yMax + 8f));
            DrawSymbolGrid(gridRect);
        }

        private void DrawSearchRow(Rect rect)
        {
            Rect labelRect = new Rect(rect.x, rect.y + 5f, 70f, 24f);
            Widgets.Label(labelRect, "BNF_DecalSearchLabel".Translate());

            Rect fieldRect = new Rect(rect.x + 74f, rect.y, rect.width - 74f - 32f - 6f, 28f);
            search = Widgets.TextField(fieldRect, search ?? "");

            Rect clearRect = new Rect(fieldRect.xMax + 6f, rect.y, 32f, 28f);
            if (Widgets.ButtonText(clearRect, "X"))
                search = "";

            TooltipHandler.TipRegion(clearRect, "BNF_DecalSearchClearTip".Translate());
        }

        private void DrawSymbolGrid(Rect rect)
        {
            IEnumerable<DecalSymbolDef> filtered = allSymbols;

            if (!search.NullOrEmpty())
            {
                string s = search.ToLowerInvariant();
                filtered = filtered.Where(d =>
                    (!d.label.NullOrEmpty() && d.label.ToLowerInvariant().Contains(s)) ||
                    (!d.defName.NullOrEmpty() && d.defName.ToLowerInvariant().Contains(s)) ||
                    (!d.path.NullOrEmpty() && d.path.ToLowerInvariant().Contains(s)));
            }

            var list = filtered.ToList();

            float cell = 58f;
            float pad = 8f;
            int cols = Mathf.Max(1, Mathf.FloorToInt((rect.width + pad) / (cell + pad)));

            float viewHeight = Mathf.CeilToInt(list.Count / (float)cols) * (cell + pad) + 10f;
            Rect view = new Rect(rect.x, rect.y, rect.width - 16f, viewHeight);

            Widgets.BeginScrollView(rect, ref symbolScroll, view);

            float x0 = view.x;
            float y0 = view.y;

            for (int i = 0; i < list.Count; i++)
            {
                int col = i % cols;
                int row = i / cols;

                Rect cellRect = new Rect(x0 + col * (cell + pad), y0 + row * (cell + pad), cell, cell);

                var def = list[i];
                Texture2D tex = null;
                if (!def.path.NullOrEmpty())
                    tex = ContentFinder<Texture2D>.Get(def.path, false);

                Widgets.DrawBoxSolid(cellRect, new Color(0.12f, 0.12f, 0.12f, 1f));
                Widgets.DrawBox(cellRect);

                if (tex != null)
                {
                    GUI.color = profile.Active ? profile.SymbolColor : new Color(1f, 1f, 1f, 0.35f);
                    GUI.DrawTexture(cellRect.ContractedBy(6f), tex);
                    GUI.color = Color.white;
                }

                bool selected = (!profile.SymbolPath.NullOrEmpty() && profile.SymbolPath == (def.path ?? ""));
                if (selected && profile.Active)
                    Widgets.DrawBoxSolid(cellRect.ContractedBy(2f), new Color(1f, 1f, 1f, 0.12f));

                if (Mouse.IsOver(cellRect))
                    TooltipHandler.TipRegion(cellRect, def.LabelCap);

                if (Widgets.ButtonInvisible(cellRect))
                {
                    profile.SymbolPath = def.path ?? "";
                    profile.Active = !def.blankType;
                }
            }

            Widgets.EndScrollView();
        }
    }
}
