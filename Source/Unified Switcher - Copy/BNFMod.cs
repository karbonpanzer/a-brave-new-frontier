using System;
using UnityEngine;
using Verse;

namespace BNF.StyleSwitcher
{
    public class BNFMod : Mod
    {
        public static BNFMod Instance { get; private set; } = null!; // non-null after ctor
        public static BNFSettings Settings => Instance.settings;      // always non-null

        private BNFSettings settings;

        public BNFMod(ModContentPack content) : base(content)
        {
            Instance = this;
            settings = GetSettings<BNFSettings>() ?? new BNFSettings();
        }

        public override string SettingsCategory() => "BNF - Settings";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var list = new Listing_Standard();
            list.Begin(inRect);

            // Header and short description (matches other mod settings style)
            list.Label("BNF - Style Switcher");
            list.Gap(4f);
            list.Label("Choose which style is active for item descriptions and textures. Each pair is mutually exclusive. Use Save to persist changes. Textures may require a reload of the save to appear on map objects.");
            list.Gap(8f);

            // Master toggles (keeps original behaviour exposed)
            list.CheckboxLabeled("Enable description switching", ref settings.EnableDescriptionSwitch);
            list.Gap(4f);
            list.CheckboxLabeled("Enable texture switching", ref settings.EnableTextureSwitch);
            list.Gap(8f);

            // === Descriptions: Vanilla vs Lore ===
            bool loreIsOn = settings.UseLoreDescriptions; // true = Lore, false = Vanilla
            DrawRadioPair(
                list,
                header: "Descriptions",
                opt1Label: "Vanilla",
                opt1Desc: "Use shorter vanilla-style description sentences.",
                opt2Label: "Lore",
                opt2Desc: "Use the mod's lore paragraphs (more verbose).",
                ref loreIsOn,
                enabled: settings.EnableDescriptionSwitch
            );
            // apply in-memory if changed visually, but don't auto-write — Save persists
            if (loreIsOn != settings.UseLoreDescriptions)
                settings.UseLoreDescriptions = loreIsOn;

            list.Gap(8f);

            // === Textures: Original vs Greyscale ===
            bool greyscaleIsOn = settings.UseGreyscaleTextures; // true = Greyscale, false = Original
            DrawRadioPair(
                list,
                header: "Textures",
                opt1Label: "Original",
                opt1Desc: "Use original color textures.",
                opt2Label: "Greyscale",
                opt2Desc: "Use greyscale textures (fixes aliasing for some views).",
                ref greyscaleIsOn,
                enabled: settings.EnableTextureSwitch
            );
            if (greyscaleIsOn != settings.UseGreyscaleTextures)
                settings.UseGreyscaleTextures = greyscaleIsOn;

            list.Gap(8f);

            // Save / Reset buttons (full-width stacked — consistent with many mods)
            if (list.ButtonText("Save"))
            {
                WriteSettings();
            }

            if (list.ButtonText("Reset to defaults"))
            {
                settings.ResetToDefaults();
                // Apply and persist the reset state
                TryApplyDescriptionsNow(settings);
                TryApplyTexturesNow(settings);
                WriteSettings();
            }

            list.End();
        }

        // Keep DrawRadioPair consistent with original usage and Widgets.RadioButtonLabeled
        private static void DrawRadioPair(
            Listing_Standard listing,
            string header,
            string opt1Label, string opt1Desc,
            string opt2Label, string opt2Desc,
            ref bool currentIsSecond,
            bool enabled)
        {
            if (!header.NullOrEmpty())
            {
                listing.Gap(4f);
                listing.Label(header);
            }

            float rowH = 28f;
            GUI.enabled = enabled;

            // Option 1 (selected when currentIsSecond == false)
            Rect r1 = listing.GetRect(rowH);
            bool clickFirst = Widgets.RadioButtonLabeled(
                r1,
                $"{opt1Label} - {opt1Desc}",
                !currentIsSecond
            );

            // Option 2 (selected when currentIsSecond == true)
            Rect r2 = listing.GetRect(rowH);
            bool clickSecond = Widgets.RadioButtonLabeled(
                r2,
                $"{opt2Label} - {opt2Desc}",
                currentIsSecond
            );

            if (clickFirst && currentIsSecond) currentIsSecond = false;
            else if (clickSecond && !currentIsSecond) currentIsSecond = true;

            GUI.enabled = true;
        }

        private static void TryApplyDescriptionsNow(BNFSettings s)
        {
            try { DescriptionApplier.ApplyAll(s); }
            catch (Exception e) { Log.Warning($"[BNF] Description apply failed: {e}"); }
        }

        private static void TryApplyTexturesNow(BNFSettings s)
        {
            try { TextureApplier.ApplyAll(s); }
            catch (Exception e) { Log.Warning($"[BNF] Texture apply failed: {e}"); }
        }

        // Persist settings and trigger appliers (safe and common pattern)
        public override void WriteSettings()
        {
            base.WriteSettings(); // writes settings to file

            // Attempt to apply the saved settings to game defs in-memory.
            TryApplyDescriptionsNow(settings);
            TryApplyTexturesNow(settings);
        }
    }
}
