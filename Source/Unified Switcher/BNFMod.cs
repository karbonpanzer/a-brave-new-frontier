using System;
using System.Reflection;
using UnityEngine;
using Verse;

namespace BNF.StyleSwitcher
{
    public class BNFMod : Mod
    {
        public static BNFMod Instance { get; private set; } = null!;
        public BNFSettings settings = null!;

        // Static accessor used by other classes
        public static BNFSettings Settings => Instance?.settings ?? new BNFSettings();

        public BNFMod(ModContentPack content) : base(content)
        {
            Instance = this;
            settings = GetSettings<BNFSettings>() ?? new BNFSettings();

            // Try to apply runtime removals at startup if BNFPatcher exists.
            TryApplyRemovalsViaReflection(settings);
        }

        public override string SettingsCategory() => "A Brave New Frontier - Style Switcher";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("BNF - Style Switcher");
            listing.Gap(4f);
            listing.Label("Manage description & texture styles, and optionally remove marked armor groups (Medic/Support). Use Save to persist changes.");
            listing.Gap(8f);

            // Master toggles
            listing.CheckboxLabeled("Enable description switching", ref settings.EnableDescriptionSwitch);
            listing.Gap(4f);
            listing.CheckboxLabeled("Enable texture switching", ref settings.EnableTextureSwitch);
            listing.Gap(8f);

            // --- Descriptions radio pair (Vanilla / Lore) ---
            bool useLore = settings.UseLoreDescriptions;
            if (!"".NullOrEmpty()) { } // noop to avoid analyzer oddities

            if (!string.IsNullOrEmpty("Descriptions"))
            {
                listing.Gap(2f);
                listing.Label("Descriptions");
            }

            float rowH = 28f;
            bool prevGui = GUI.enabled;
            GUI.enabled = settings.EnableDescriptionSwitch;

            Rect rDesc1 = listing.GetRect(rowH);
            bool selectedDesc1 = !useLore; // Vanilla selected when useLore == false
            bool clickedDesc1 = Widgets.RadioButtonLabeled(rDesc1, "Vanilla - Use shorter vanilla-style description sentences.", selectedDesc1);
            if (clickedDesc1 && useLore)
            {
                useLore = false;
            }

            Rect rDesc2 = listing.GetRect(rowH);
            bool selectedDesc2 = useLore; // Lore selected when useLore == true
            bool clickedDesc2 = Widgets.RadioButtonLabeled(rDesc2, "Lore - Use the mod's lore paragraphs (more verbose).", selectedDesc2);
            if (clickedDesc2 && !useLore)
            {
                useLore = true;
            }

            GUI.enabled = prevGui;

            // Commit selection back to settings
            if (useLore != settings.UseLoreDescriptions)
                settings.UseLoreDescriptions = useLore;

            listing.Gap(8f);

            // --- Textures radio pair (Original / Greyscale) ---
            bool useGreyscale = settings.UseGreyscaleTextures;

            if (!string.IsNullOrEmpty("Textures"))
            {
                listing.Gap(2f);
                listing.Label("Textures");
            }

            prevGui = GUI.enabled;
            GUI.enabled = settings.EnableTextureSwitch;

            Rect rTex1 = listing.GetRect(rowH);
            bool selectedTex1 = !useGreyscale; // Original when false
            bool clickedTex1 = Widgets.RadioButtonLabeled(rTex1, "Original - Use original color textures.", selectedTex1);
            if (clickedTex1 && useGreyscale)
            {
                useGreyscale = false;
            }

            Rect rTex2 = listing.GetRect(rowH);
            bool selectedTex2 = useGreyscale; // Greyscale when true
            bool clickedTex2 = Widgets.RadioButtonLabeled(rTex2, "Greyscale - Use greyscale textures.", selectedTex2);
            if (clickedTex2 && !useGreyscale)
            {
                useGreyscale = true;
            }

            GUI.enabled = prevGui;

            if (useGreyscale != settings.UseGreyscaleTextures)
                settings.UseGreyscaleTextures = useGreyscale;

            listing.Gap(12f);

            // Armor removal UI (master checkbox + discovered groups)
            BNFArmorRemoval.DrawRemovalSection(listing, settings);

            listing.Gap(10f);

            // Save and Reset
            if (listing.ButtonText("Save"))
            {
                WriteSettings();
            }

            if (listing.ButtonText("Reset to defaults"))
            {
                settings.ResetToDefaults();

                // Apply runtime marks and persist
                TryApplyRemovalsViaReflection(settings);
                WriteSettings();
            }

            listing.End();
        }

        public override void WriteSettings()
        {
            base.WriteSettings();

            try { DescriptionApplier.ApplyAll(settings); }
            catch (Exception e) { Log.Warning($"[BNF] Description apply failed: {e}"); }

            try { TextureApplier.ApplyAll(settings); }
            catch (Exception e) { Log.Warning($"[BNF] Texture apply failed: {e}"); }

            // Apply runtime removal marks if BNFPatcher exists
            TryApplyRemovalsViaReflection(settings);
        }

        /// <summary>
        /// Safely tries to invoke BNFPatcher.ApplyRemovals(BNFSettings) via reflection.
        /// This avoids compile-time dependency on BNFPatcher.cs — if that file
        /// is present in the project the method is called; otherwise this is a no-op.
        /// </summary>
        private static void TryApplyRemovalsViaReflection(BNFSettings s)
        {
            if (s == null) return;

            try
            {
                // Try to find the BNFPatcher type in any loaded assembly
                Type patcherType = Type.GetType("BNF.StyleSwitcher.BNFPatcher");
                if (patcherType == null)
                {
                    foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        patcherType = a.GetType("BNF.StyleSwitcher.BNFPatcher");
                        if (patcherType != null) break;
                    }
                }

                if (patcherType == null) return;

                MethodInfo mi = patcherType.GetMethod("ApplyRemovals", BindingFlags.Public | BindingFlags.Static);
                if (mi == null) return;

                mi.Invoke(null, new object[] { s });
                Log.Message("[BNF] BNFPatcher.ApplyRemovals invoked via reflection.");
            }
            catch (Exception ex)
            {
                Log.Warning($"[BNF] Failed to invoke BNFPatcher.ApplyRemovals: {ex}");
            }
        }
    }
}
