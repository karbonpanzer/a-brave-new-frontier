using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace BNF.StyleSwitcher
{
    public class BNFMod : Mod
    {
        public static BNFMod? Instance { get; private set; }

        private readonly BNFSettings settings;

        public static BNFSettings SettingsOrDefault => Instance?.settings ?? new BNFSettings();

        public BNFMod(ModContentPack content) : base(content)
        {
            Instance = this;
            settings = GetSettings<BNFSettings>() ?? new BNFSettings();

            // Single, non-spammy log line.
            Log.Message("[BNF] Description Switcher initialized.");
        }

        public override string SettingsCategory() => "BNF - Mod Settings";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            Text.Font = GameFont.Medium;
            Rect headerRect = listing.GetRect(34f);
            Widgets.Label(headerRect, "A Brave New Frontier Settings");
            Text.Font = GameFont.Small;
            listing.Gap(6f);

            listing.Label("Descriptions");
            listing.Gap(4f);

            const float rowH = 28f;

            const string vanillaFull = "Vanilla: Uses a style similar to vanilla Rimworld items.";
            const string loreFull = "Lore: My personal choices which adds great deal of fan-fiction to the mod items.";

            bool newUseLore = settings.UseLoreDescriptions;

            Rect rDesc1 = listing.GetRect(rowH);
            if (Widgets.RadioButtonLabeled(rDesc1, vanillaFull, !settings.UseLoreDescriptions))
                newUseLore = false;

            Rect rDesc2 = listing.GetRect(rowH);
            if (Widgets.RadioButtonLabeled(rDesc2, loreFull, settings.UseLoreDescriptions))
                newUseLore = true;

            if (newUseLore != settings.UseLoreDescriptions)
            {
                settings.UseLoreDescriptions = newUseLore;
                TryWriteAndApply(showAppliedMessage: true);
            }

            listing.Gap(12f);

            Rect btnReset = listing.GetRect(34f);
            if (Widgets.ButtonText(btnReset, "Reset to defaults"))
            {
                settings.ResetToDefaults();
                TryWriteAndApply(showAppliedMessage: false);
                Messages.Message("BNF: Settings reset to defaults.", MessageTypeDefOf.TaskCompletion);
            }

            listing.End();
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            DescriptionApplier.ApplyAll(settings);
        }

        private void TryWriteAndApply(bool showAppliedMessage)
        {
            try
            {
                WriteSettings();
                if (showAppliedMessage)
                {
                    Messages.Message(
                        "BNF: Description changes apply instantly and do not require reloading the game.",
                        MessageTypeDefOf.TaskCompletion
                    );
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[BNF] Failed to write/apply description settings: {e}");
            }
        }
    }
}
