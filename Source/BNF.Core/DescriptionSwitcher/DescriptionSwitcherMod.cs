using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace BNF.Core.DescriptionSwitcher
{

    public class BnfMod : Mod
    {
        private static BnfMod? _instance;

        private readonly BnfSettings _settings;
        
        public static BnfSettings Settings => Instance._settings;

        private static BnfMod Instance =>
            _instance ?? throw new InvalidOperationException("[BNF] BnfMod accessed before it was constructed.");

        public BnfMod(ModContentPack content) : base(content)
        {
            _instance = this;
            _settings = GetSettings<BnfSettings>();

            Log.Message("[BNF] Description Switcher loaded (settings initialized)");
        }

        public override string SettingsCategory() => "BNF - Settings";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            Text.Font = GameFont.Medium;
            Rect headerRect = listing.GetRect(34f);
            Widgets.Label(headerRect, "A Brave New Frontier - Style Switcher");
            Text.Font = GameFont.Small;
            listing.Gap(6f);

            listing.Label("Descriptions");

            const float rowH = 28f;
            const string vanillaFull =
                "Vanilla: Uses standard gameplay-focused descriptions that emphasize stats and function";
            const string loreFull =
                "Lore: Replaces or augments descriptions with BNF lore and flavor text that emphasize story and setting";

            bool oldUseLore = _settings.UseLoreDescriptions;
            bool newUseLore = oldUseLore;

            Rect rDesc1 = listing.GetRect(rowH);
            if (Widgets.RadioButtonLabeled(rDesc1, vanillaFull, !oldUseLore))
                newUseLore = false;

            Rect rDesc2 = listing.GetRect(rowH);
            if (Widgets.RadioButtonLabeled(rDesc2, loreFull, oldUseLore))
                newUseLore = true;

            if (newUseLore != oldUseLore)
            {
                _settings.UseLoreDescriptions = newUseLore;
                TryWriteAndApply("BNF: Description changes apply instantly and do not require reloading the game.");
            }

            listing.Gap(16f);

            Rect btnReset = listing.GetRect(34f);
            if (Widgets.ButtonText(btnReset, "Reset to defaults"))
            {
                _settings.ResetToDefaults();
                TryWriteAndApply("BNF: Settings reset to defaults.");
            }

            listing.End();
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            TryApplyDescriptions();
        }

        private void TryWriteAndApply(string successMessage)
        {
            try
            {
                WriteSettings();
                Messages.Message(successMessage, MessageTypeDefOf.TaskCompletion);
            }
            catch (Exception e)
            {
                Log.Warning($"[BNF] Failed to write/apply settings: {e}");
            }
        }

        private void TryApplyDescriptions()
        {
            try
            {
                DescriptionApplier.ApplyAll(_settings);
            }
            catch (Exception e)
            {
                Log.Warning($"[BNF] Description apply failed: {e}");
            }
        }
    }
}
