using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace BNF.StyleSwitcher
{
    public class BNFMod : Mod
    {
        public static BNFMod Instance { get; private set; } = null!;
        public BNFSettings settings = null!;

        public static BNFSettings Settings => Instance?.settings ?? new BNFSettings();

        public BNFMod(ModContentPack content) : base(content)
        {
            Instance = this;
            settings = GetSettings<BNFSettings>() ?? new BNFSettings();
        }

        public override string SettingsCategory() => "BNF - Settings";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            // Header
            Text.Font = GameFont.Medium;
            Rect headerRect = listing.GetRect(34f);
            Widgets.Label(headerRect, "A Brave New Frontier - Style Switcher");
            Text.Font = GameFont.Small;
            listing.Gap(6f);

            // Description toggles
            float rowH = 28f;
            listing.Label("Descriptions");
            bool useLore = settings.UseLoreDescriptions;

            // Full descriptions always shown after the option name
            const string vanillaFull = "Vanilla: Uses standard gameplay-focused descriptions that emphasize stats and function";
            const string loreFull = "Lore: Replaces or augments descriptions with BNF lore and flavor text that emphasize story and setting";

            bool newUseLore = useLore;

            bool selectedVanilla = !useLore;
            string labelVanilla = vanillaFull;
            Rect rDesc1 = listing.GetRect(rowH);
            if (Widgets.RadioButtonLabeled(rDesc1, labelVanilla, selectedVanilla))
                newUseLore = false;

            bool selectedLore = useLore;
            string labelLore = loreFull;
            Rect rDesc2 = listing.GetRect(rowH);
            if (Widgets.RadioButtonLabeled(rDesc2, labelLore, selectedLore))
                newUseLore = true;

            // If descriptions changed, persist, apply immediately, and notify user no reload is required
            if (newUseLore != settings.UseLoreDescriptions)
            {
                settings.UseLoreDescriptions = newUseLore;
                try
                {
                    WriteSettings(); // persists and runs DescriptionApplier
                    Messages.Message(
                        "BNF: Description changes apply instantly and do not require reloading the game.",
                        MessageTypeDefOf.TaskCompletion
                    );
                }
                catch (Exception e)
                {
                    Log.Warning($"[BNF] Failed to write/apply description settings: {e}");
                }
            }

            listing.Gap(8f);

            // Texture toggles
            listing.Label("Textures");
            bool useGreyscale = settings.UseGreyscaleTextures;

            const string originalFull = "Original: Keeps original color textures from the base game and mods";
            const string greyscaleFull = "Greyscale: Applies BNF greyscale variants for a unified monochrome aesthetic";

            bool newUseGreyscale = useGreyscale;

            bool selectedOriginal = !useGreyscale;
            string labelOriginal = originalFull;
            Rect rTex1 = listing.GetRect(rowH);
            if (Widgets.RadioButtonLabeled(rTex1, labelOriginal, selectedOriginal))
                newUseGreyscale = false;

            bool selectedGreyscale = useGreyscale;
            string labelGreyscale = greyscaleFull;
            Rect rTex2 = listing.GetRect(rowH);
            if (Widgets.RadioButtonLabeled(rTex2, labelGreyscale, selectedGreyscale))
                newUseGreyscale = true;

            // If texture choice changed, persist and apply defs for next load, and notify user that a reload is required to see the visuals
            if (newUseGreyscale != settings.UseGreyscaleTextures)
            {
                settings.UseGreyscaleTextures = newUseGreyscale;
                try
                {
                    WriteSettings(); // persists and runs TextureApplier so defs are updated for next load
                }
                catch (Exception e)
                {
                    Log.Warning($"[BNF] Failed to write/apply texture settings: {e}");
                }

                Messages.Message(
                    "BNF: Texture changes require reloading the save or restarting the game to take full effect.",
                    MessageTypeDefOf.TaskCompletion
                );
            }

            listing.Gap(12f);

            // Reset to defaults near bottom
            // Give a bit of space so the reset button sits lower in the window
            listing.Gap(8f);
            Rect btnReset = listing.GetRect(34f);
            if (Widgets.ButtonText(btnReset, "Reset to defaults"))
            {
                // reset, persist, and apply
                settings.ResetToDefaults();
                try
                {
                    WriteSettings();
                    Messages.Message("BNF: Settings reset to defaults.", MessageTypeDefOf.TaskCompletion);
                }
                catch (Exception e)
                {
                    Log.Warning($"[BNF] Failed to reset/write settings: {e}");
                }
            }

            listing.End();
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            try { DescriptionApplier.ApplyAll(settings); } catch (Exception e) { Log.Warning($"[BNF] Description apply failed: {e}"); }
            try { TextureApplier.ApplyAll(settings); } catch (Exception e) { Log.Warning($"[BNF] Texture apply failed: {e}"); }
        }
    }
}
