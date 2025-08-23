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

            list.Label("Choose which style is active for item descriptions and textures. " +
                       "Each pair is mutually exclusive.");
            list.Gap(6f);

            // === Descriptions: Vanilla vs Lore ===
            bool loreIsOn = settings.UseLoreDescriptions; // true = Lore, false = Vanilla
            DrawRadioPair(
                list,
                header: "Descriptions",
                opt1Label: "Vanilla",
                opt1Desc: "Use a more vanilla styled descriptions for items, rather than a paragraph of writing, this is a single sentence.",
                opt2Label: "Lore",
                opt2Desc: "Use the lore that I built up for this mod for descriptions which while flavorful is not everyones taste.",
                ref loreIsOn,
                enabled: true
            );

            if (loreIsOn != settings.UseLoreDescriptions)
            {
                settings.UseLoreDescriptions = loreIsOn;
                TryApplyDescriptionsNow(settings);
            }

            list.Gap(8f);

            // === Textures: Original vs Greyscale ===
            bool greyscaleIsOn = settings.UseGreyscaleTextures; // true = Greyscale, false = Original
            DrawRadioPair(
                list,
                header: "Textures",
                opt1Label: "Original",
                opt1Desc: "Use the original color textures which possess aliasing issues which can be noticed if using camera+.",
                opt2Label: "Greyscale",
                opt2Desc: "Use the greyscaled textures which fix the aliasing issues but you lack those brass and browns in the textures.",
                ref greyscaleIsOn,
                enabled: true
            );

            if (greyscaleIsOn != settings.UseGreyscaleTextures)
            {
                settings.UseGreyscaleTextures = greyscaleIsOn;
                TryApplyTexturesNow(settings);
            }

            list.Gap(6f);
            list.Label("After making your selection, just reload the save and it will load new textures. Descriptions however will automatically do it.");

            list.End();

            settings.Write(); // persist on close
        }

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
                $"{opt1Label} — {opt1Desc}",
                !currentIsSecond
            );

            // Option 2 (selected when currentIsSecond == true)
            Rect r2 = listing.GetRect(rowH);
            bool clickSecond = Widgets.RadioButtonLabeled(
                r2,
                $"{opt2Label} — {opt2Desc}",
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
    }
}
