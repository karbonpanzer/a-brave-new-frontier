using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace BNF.StyleSwitcher
{
	public class BNF_StyleSwitcherMod : Mod
	{
		public static BNF_StyleSwitcherMod Instance { get; private set; } = null!;
		public BNFSettings settings = null!;

		private const int WeaponCheckboxColumns = 3;
		private const float CheckboxHeight = 28f;
		private const float CheckboxGap = 6f;

		public static BNFSettings Settings => Instance?.settings ?? new BNFSettings();

		public BNF_StyleSwitcherMod(ModContentPack content) : base(content)
		{
			Instance = this;
			settings = GetSettings<BNFSettings>() ?? new BNFSettings();

			// Defer heavy work until after defs are fully initialized to avoid DefOf/uninitialized issues
			LongEventHandler.QueueLongEvent(() => {
				try { BNF_WeaponDisabler.ApplyAll(settings); }
				catch (Exception e) { Log.Warning($"[BNF] WeaponDisabler startup failed (deferred): {e}"); }
			}, "BNF_ApplyWeaponSettings", false, null);
		}

		public override string SettingsCategory() => "BNF - Settings";

		public override void DoSettingsWindowContents(Rect inRect)
		{
			var listing = new Listing_Standard();
			listing.Begin(inRect);

			// Header
			Text.Font = GameFont.Medium;
			Rect headerRect = listing.GetRect(34f);
			Widgets.Label(headerRect, "A Brave New Frontier - Mod Options");
			Text.Font = GameFont.Small;
			listing.Gap(6f);

			// Description toggles
			float rowH = 28f;
			listing.Label("Descriptions");
			bool useLore = settings.UseLoreDescriptions;

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

			// Weapons section - grid, 3 columns, no scrollbar
			
			// Weapons section - grid, 3 columns, no scrollbar
			listing.Label("BNF Weapons");
			bool anyChanged = false;

			// Enable all checkbox (checked = spawn)
			bool newEnableAll = settings.EnableAllWeapons;
			Rect rEnableAll = listing.GetRect(rowH);
			Widgets.CheckboxLabeled(rEnableAll, "Enable all BNF weapons (checked = spawn)", ref newEnableAll);
			if (newEnableAll != settings.EnableAllWeapons)
			{
				settings.EnableAllWeapons = newEnableAll;
				if (newEnableAll)
				{
					// enable all: clear explicit disabled list
					settings.DisabledWeaponDefNames?.Clear();
				}
				else
				{
					// disable all: mark every BNF weapon as disabled
					settings.DisabledWeaponDefNames ??= new System.Collections.Generic.List<string>();
					settings.DisabledWeaponDefNames.Clear();
					foreach (var d in DefDatabase<ThingDef>.AllDefsListForReading)
					{
						if (d.GetModExtension<BNFWeaponExtension>() != null)
							settings.DisabledWeaponDefNames.Add(d.defName);
					}
				}
				anyChanged = true;
			}

			listing.Gap(6f);

			// Collect the weapon defs that have the mod extension
			var weaponDefs = DefDatabase<ThingDef>.AllDefsListForReading
				.Where(d => d.GetModExtension<BNFWeaponExtension>() != null)
				.OrderBy(d => (d.GetModExtension<BNFWeaponExtension>()?.displayName ?? d.label).ToLower())
				.ToList();

			int count = weaponDefs.Count;
			int cols = Math.Max(1, WeaponCheckboxColumns);
			int rows = (count + cols - 1) / cols;
			float contentWidth = Math.Max(1f, listing.ColumnWidth); // use Listing_Standard's column width
			float totalGapWidth = (cols - 1) * CheckboxGap;
			float checkboxWidth = Math.Max(120f, (contentWidth - totalGapWidth) / cols); // ensure a reasonable min width

			float sectionHeight = rows * (CheckboxHeight + CheckboxGap);
			Rect weaponsRect = listing.GetRect(sectionHeight);

			// draw grid
			for (int r = 0; r < rows; r++)
			{
				for (int c = 0; c < cols; c++)
				{
					int idx = r * cols + c;
					if (idx >= count) break;

					var def = weaponDefs[idx];
					var wext = def.GetModExtension<BNFWeaponExtension>();
					if (wext == null) continue;
					string display = string.IsNullOrEmpty(wext.displayName) ? def.label.CapitalizeFirst() : wext.displayName;

					float x = weaponsRect.x + c * (checkboxWidth + CheckboxGap);
					float y = weaponsRect.y + r * (CheckboxHeight + CheckboxGap);
					Rect boxRect = new Rect(x, y, checkboxWidth, CheckboxHeight);

					// compute whether this weapon is currently enabled (checked = enabled)
					bool wasEnabled;
					if (!settings.EnableAllWeapons) wasEnabled = false;
					else wasEnabled = !(settings.DisabledWeaponDefNames?.Contains(def.defName) ?? false);
					bool nowEnabled = wasEnabled;
					Widgets.CheckboxLabeled(boxRect, display, ref nowEnabled);
					if (nowEnabled != wasEnabled)
					{
						// If global was disabled and user enabled an individual, switch global to enabled
						if (!settings.EnableAllWeapons && nowEnabled)
						{
							settings.EnableAllWeapons = true;
							// remove this def from disabled list so it's enabled
							settings.DisabledWeaponDefNames?.Remove(def.defName);
						}
						else
						{
							if (nowEnabled)
							{
								// enable: remove from disabled list
								settings.DisabledWeaponDefNames?.Remove(def.defName);
							}
							else
							{
								// disable: add to disabled list
								settings.DisabledWeaponDefNames ??= new System.Collections.Generic.List<string>();
								if (!settings.DisabledWeaponDefNames.Contains(def.defName))
									settings.DisabledWeaponDefNames.Add(def.defName);
							}
						}
						anyChanged = true;
					}
				}
			}

			if (anyChanged)
			{
				try
				{
					// Persist settings but do not run the live disabler — require reload for weapon changes
					base.WriteSettings();
					Messages.Message(
						"BNF: Weapon changes require reloading the save or restarting the game to take full effect.",
						MessageTypeDefOf.TaskCompletion
					);
				}
				catch (Exception e)
				{
					Log.Warning($"[BNF] Failed to write weapon settings: {e}");
				}
			}


			listing.Gap(12f);

			// Reset to defaults near bottom
			listing.Gap(8f);
			Rect btnReset = listing.GetRect(34f);
			if (Widgets.ButtonText(btnReset, "Reset to defaults"))
			{
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
			// Weapon changes require reload; do not apply live here.
		}
	}
}
