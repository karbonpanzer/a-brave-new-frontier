using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

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
            catch (Exception e)
            {
                Log.Error("[BNF] Decal System failed to load:\n" + e);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class PatchPawnGetGizmosDecals
    {
        public static void Postfix(
            [HarmonyArgument("__instance")] Pawn instance,
            [HarmonyArgument("__result")] ref IEnumerable<Gizmo> result)
        {
            var pawn = instance;
            if (pawn.Faction != Faction.OfPlayerSilentFail) return;
            if (!DecalUtil.IsHumanlikePawn(pawn)) return;
            if (!DecalUtil.PawnHasAnyDecalApparel(pawn)) return;

            var cmd = new Command_Action
            {
                defaultLabel = "BNF_StyleDecalsGizmo".Translate(pawn.LabelCap),
                defaultDesc = "BNF_StyleDecalsDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/CustomizeDecal"),
                action = () => Find.WindowStack.Add(new DialogEditDecals(pawn))
            };

            result = result.Concat([cmd]);
        }
    }

    public sealed class DialogEditDecals : Window
    {
        private readonly Pawn _pawn;

        private DecalProfile _profile;
        private readonly DecalProfile _original;

        private readonly List<DecalSymbolDef> _symbols;
        private int _selectedIndex;
        private DecalSymbolDef? _selectedSymbol;

        private bool _committed;

        private Vector2 _scrollPos;
        private float _viewRectHeight;

        private List<Color>? _allColors;

        public override Vector2 InitialSize => new Vector2(600f, 680f);

        public DialogEditDecals(Pawn pawn)
        {
            _pawn = pawn;

            forcePause = false;
            closeOnAccept = false;
            closeOnCancel = false;
            absorbInputAroundWindow = true;
            preventCameraMotion = false;
            doCloseX = true;

            _profile = DecalUtil.ReadProfileFrom(_pawn);
            _original = _profile;

            _symbols = DecalUtil.AllSymbols();
            _selectedIndex = FindSymbolIndex(_profile.SymbolPath);
            SyncSelection();

            DecalUtil.BeginLiveEdit(_pawn);
            DecalUtil.SetLiveEdit(_pawn, _profile);
        }

        public override void Close(bool doCloseSound = true)
        {
            DecalUtil.EndLiveEdit(_pawn, _committed);
            base.Close(doCloseSound);
        }

        public override void DoWindowContents(Rect inRect)
        {

            if (_pawn.Destroyed)
            {
                Close(false);
                return;
            }

            float outerPad = 12f;
            float footerH = 64f;

            Text.Font = GameFont.Medium;

            Rect titleRect = new Rect(
                inRect.x + outerPad,
                inRect.y,
                inRect.width - outerPad * 2f,
                Text.LineHeight * 2f);

            Widgets.Label(titleRect, "BNF_StyleDecalsTitle".Translate(_pawn.Name.ToStringShort));

            Text.Font = GameFont.Small;

            Rect content = new Rect(
                inRect.x + outerPad,
                titleRect.yMax + 6f,
                inRect.width - outerPad * 2f,
                inRect.height - (titleRect.height + 6f) - footerH);

            Rect footer = new Rect(
                inRect.x + outerPad,
                inRect.yMax - footerH,
                inRect.width - outerPad * 2f,
                footerH);

            DrawMainUI(content);
            DrawBottomButtons(footer);
        }

        private void DrawMainUI(Rect rect)
        {
            Widgets.DrawMenuSection(rect);

            float innerPad = 14f;
            rect = rect.ContractedBy(innerPad);

            Rect viewRect = new Rect(rect.x, rect.y, rect.width, _viewRectHeight <= 0f ? 600f : _viewRectHeight);
            Widgets.BeginScrollView(rect, ref _scrollPos, viewRect);

            float y = rect.y;

            y = DrawSymbolSetup(new Rect(rect.x, y, viewRect.width, 210f)) + 12f;
            y = DrawColorSetup(new Rect(rect.x, y, viewRect.width, 280f));

            if (Event.current.type == EventType.Layout)
                _viewRectHeight = y - rect.y;

            Widgets.EndScrollView();
        }

        private float DrawSymbolSetup(Rect rect)
        {
            float y = rect.y;     
            float lineH = 30f;

            Rect enabledLine = new Rect(rect.x, y, rect.width * 0.55f, lineH);
            bool before = _profile.Active;

            float checkSize = 24f;
            Rect checkRect = new Rect(
                enabledLine.x,
                enabledLine.y + (enabledLine.height - checkSize) * 0.5f,
                checkSize,
                checkSize);

            Widgets.Checkbox(checkRect.position, ref _profile.Active, checkSize);

            Rect enabledLabel = new Rect(
                checkRect.xMax + 4f,
                enabledLine.y,
                enabledLine.width - (checkSize + 4f),
                enabledLine.height);

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(enabledLabel, "Enabled".Translate());
            Text.Anchor = TextAnchor.UpperLeft;

            if (before != _profile.Active) PushLive();

            float btnW = rect.width * 0.40f;
            Rect randomRect = new Rect(rect.xMax - btnW, y, btnW, lineH);
            if (Widgets.ButtonText(randomRect, "BNF_Decals_RandomSymbol".Translate()))
            {
                if (_symbols.Count > 0)
                {
                    _selectedIndex = Rand.Range(0, _symbols.Count);
                    SyncSelection();
                    PushLive();
                }
            }

            y = enabledLine.yMax + 12f;

            Rect row = new Rect(rect.x, y, rect.width, 36f);
            if (_symbols.Count == 0)
            {
                Widgets.Label(row, "BNF_Decals_NoSymbolsFound".Translate());
                return row.yMax;
            }

            MakeFloatOptionButtons(
                row,
                () =>
                {
                    _selectedIndex--;
                    if (_selectedIndex < 0) _selectedIndex = _symbols.Count - 1;
                    SyncSelection();
                    PushLive();
                },
                () =>
                {
                    FloatMenuUtility.MakeMenu(
                        _symbols,
                        entry => entry.LabelCap,
                        variant => () =>
                        {
                            _selectedIndex = _symbols.FindIndex(x => x != null && x.Path == variant.Path);
                            if (_selectedIndex < 0) _selectedIndex = 0;
                            SyncSelection();
                            PushLive();
                        });
                },
                _selectedSymbol?.LabelCap ?? "BNF_Decals_NoSelection".Translate(),
                () =>
                {
                    _selectedIndex++;
                    if (_selectedIndex >= _symbols.Count) _selectedIndex = 0;
                    SyncSelection();
                    PushLive();
                });

            return row.yMax;
        }

        private float DrawColorSetup(Rect rect)
        {
            Rect header = new Rect(rect.x, rect.y, rect.width, 36f);
            Text.Font = GameFont.Medium;
            Widgets.Label(header, "BNF_Decals_Color".Translate());
            Text.Font = GameFont.Small;

            float y = header.yMax + 10f;

            Color color = _profile.SymbolColor;

            const int boxSize = 28;
            const int columns = 6;
            const float gap = 10f;

            Rect selectorRect = new Rect(rect.x, y, rect.width, 9999f);
            Widgets.ColorSelector(selectorRect, ref color, AllColors(), out float usedHeight, null, boxSize, columns);

            y = selectorRect.y + usedHeight + gap;

            float buttonHeight = 36f;
            Rect buttonRow = new Rect(rect.x, y, rect.width, buttonHeight);
            DrawColorButtons(buttonRow, ref color);

            y = buttonRow.yMax;

            if (!_profile.SymbolColor.IndistinguishableFrom(color))
            {
                SetColor(color);
                PushLive();
            }

            return y;
        }

        private void DrawColorButtons(Rect rect, ref Color color)
        {
            const float gap = 24f;
            float x = rect.x;

            bool hasIdeo = TryGetIdeoColor(_pawn, out Color ideoColor);
            bool hasFav = TryGetFavoriteColor(_pawn, out Color favColor);

            string ideoLabel = "BNF_Decals_SetIdeoColor".Translate();
            string rndLabel = "BNF_Decals_RandomColor".Translate();
            string favLabel = "BNF_Decals_SetFavoriteColor".Translate();

            float ideoW = Text.CalcSize(ideoLabel).x + 30f;
            float rndW = Text.CalcSize(rndLabel).x + 30f;
            float favW = Text.CalcSize(favLabel).x + 30f;

            float total = ideoW + rndW + favW + gap * 2f;
            if (total > rect.width)
            {
                float scale = rect.width / total;
                ideoW *= scale;
                rndW *= scale;
                favW *= scale;
            }

            Rect ideoRect = new Rect(x, rect.y, ideoW, rect.height);
            x += ideoW + gap;

            Rect rndRect = new Rect(x, rect.y, rndW, rect.height);
            x += rndW + gap;

            Rect favRect = new Rect(x, rect.y, favW, rect.height);

            if (!hasIdeo) GUI.color = new Color(1f, 1f, 1f, 0.45f);
            if (Widgets.ButtonText(ideoRect, ideoLabel) && hasIdeo)
            {
                color = ideoColor;
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }
            GUI.color = Color.white;

            if (Widgets.ButtonText(rndRect, rndLabel))
            {
                var pool = AllColors();
                if (pool.Count > 0)
                {
                    color = pool[Rand.Range(0, pool.Count)];
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                }
            }

            if (!hasFav) GUI.color = new Color(1f, 1f, 1f, 0.45f);
            if (Widgets.ButtonText(favRect, favLabel) && hasFav)
            {
                color = favColor;
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }
            GUI.color = Color.white;
        }

        private void DrawBottomButtons(Rect rect)
        {
            float buttonH = 28f;
            float gapX = 10f;
            float w = (rect.width - gapX * 2f) / 3f;

            float y = rect.yMax - buttonH - 15f;

            Rect reset = new Rect(rect.x, y, w, buttonH);
            Rect apply = new Rect(reset.xMax + gapX, y, w, buttonH);
            Rect close = new Rect(apply.xMax + gapX, y, w, buttonH);

            if (Widgets.ButtonText(reset, "BNF_Decals_Reset".Translate()))
            {
                _profile = _original;
                _selectedIndex = FindSymbolIndex(_profile.SymbolPath);
                SyncSelection();
                PushLive();
            }

            if (Widgets.ButtonText(apply, "BNF_Decals_Apply".Translate()))
            {
                _committed = true;
                Close();
                return;
            }

            if (Widgets.ButtonText(close, "Close".Translate()))
            {
                _committed = false;
                Close();
            }
        }

        private List<Color> AllColors()
        {
            if (_allColors != null) return _allColors;

            _allColors =
            [
                new Color(0.08f, 0.8f, 0.08f),
                new Color(0.15f, 0.15f, 0.15f),
                new Color(0.9f, 0.9f, 0.9f),
                Color.white,
                new Color(0.5f, 0.5f, 0.25f),
                new Color(0.9f, 0.9f, 0.5f),
                new Color(0.9f, 0.8f, 0.5f),
                new Color(0.45f, 0.2f, 0.2f),
                new Color(0.5f, 0.25f, 0.25f),
                new Color(0.9f, 0.5f, 0.5f),
                new Color(0.15f, 0.28f, 0.43f),
                new Color(0.98f, 0.92f, 0.84f),
                new Color(0.87f, 0.96f, 0.91f),
                new Color(0.94f, 0.87f, 0.96f),
                new Color(0.96f, 0.87f, 0.87f),
                new Color(0.87f, 0.94f, 0.96f),
                new Color(0.05f, 0.08f, 0.20f),
                new Color(0.25f, 0.35f, 0.45f),
                new Color(0.05f, 0.45f, 0.45f),
                new Color(0.10f, 0.75f, 0.85f),
                new Color(0.75f, 0.05f, 0.55f),
                new Color(0.60f, 0.05f, 0.10f),
                new Color(0.85f, 0.35f, 0.05f),
            ];

            if (TryGetIdeoColor(_pawn, out Color ideo) && !_allColors.Any(x => x.IndistinguishableFrom(ideo)))
                _allColors.Add(ideo);

            if (TryGetFavoriteColor(_pawn, out Color fav) && !_allColors.Any(x => x.IndistinguishableFrom(fav)))
                _allColors.Add(fav);

            foreach (var colDef in DefDatabase<ColorDef>.AllDefs.Where(x => x.colorType == ColorType.Ideo || x.colorType == ColorType.Misc))
            {
                if (!_allColors.Any(x => x.IndistinguishableFrom(colDef.color)))
                    _allColors.Add(colDef.color);
            }

            _allColors.Sort((a, b) =>
            {
                Color.RGBToHSV(a, out float ah, out float asat, out float av);
                Color.RGBToHSV(b, out float bh, out float bsat, out float bv);

                int c = ah.CompareTo(bh);
                if (c != 0) return c;
                c = asat.CompareTo(bsat);
                if (c != 0) return c;
                return av.CompareTo(bv);
            });

            const int maxColors = 66;
            if (_allColors.Count > maxColors)
                _allColors = _allColors.Take(maxColors).ToList();

            return _allColors;
        }

        private int FindSymbolIndex(string path)
        {
            if (path.NullOrEmpty() || _symbols.Count == 0) return 0;

            int idx = _symbols.FindIndex(d => d != null && d.Path == path);
            return idx >= 0 ? idx : 0;
        }

        private void SyncSelection()
        {
            if (_symbols.Count == 0)
            {
                _selectedSymbol = null;
                _profile.SymbolPath = "";
                _profile.Active = false;
                return;
            }

            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, _symbols.Count - 1);
            _selectedSymbol = _symbols[_selectedIndex];

            if (_selectedSymbol != null)
            {
                _profile.SymbolPath = _selectedSymbol.Path;
                _profile.Active = true;
            }
            else
            {
                _profile.SymbolPath = "";
                _profile.Active = false;
            }
        }

        private void SetColor(Color c)
        {
            _profile.SymbolColor = c;
            if (!_profile.Active) _profile.Active = true;
        }

        private void PushLive()
        {
            DecalUtil.SetLiveEdit(_pawn, _profile);
        }

        private static bool TryGetIdeoColor(Pawn? pawn, out Color c)
        {
            c = Color.white;
            if (!ModsConfig.IdeologyActive) return false;
            if (pawn?.Ideo == null) return false;
            if (Find.IdeoManager.classicMode) return false;
            c = pawn.Ideo.ApparelColor;
            return true;
        }

        private static bool TryGetFavoriteColor(Pawn? pawn, out Color c)
        {
            c = Color.white;
            if (!ModsConfig.IdeologyActive) return false;
            if (pawn?.story == null) return false;
            if (pawn.DevelopmentalStage.Baby()) return false;
            ColorDef? def = pawn.story.favoriteColor;
            if (def == null) return false;
            c = def.color;
            return true;
        }

        private static void MakeFloatOptionButtons(Rect rect, Action leftAction, Action centerAction, string centerButtonName, Action rightAction)
        {
            Rect leftRect = new Rect(rect.x, rect.y, 44f, rect.height);
            Rect rightRect = new Rect(rect.xMax - 44f, rect.y, 44f, rect.height);
            Rect midRect = new Rect(leftRect.xMax + 8f, rect.y, rect.width - 44f * 2f - 16f, rect.height);

            if (Widgets.ButtonText(leftRect, "<")) leftAction();
            if (Widgets.ButtonText(midRect, centerButtonName)) centerAction();
            if (Widgets.ButtonText(rightRect, ">")) rightAction();
        }
    }
}