using System.Collections.Generic;
using Verse;

namespace BNF.StyleSwitcher
{
	public class BNFSettings : ModSettings
	{
		public bool UseLoreDescriptions = true;
		public bool UseGreyscaleTextures = false;

		// weapon enabling: checked => enabled/spawn. Default true = enabled.
		public bool EnableAllWeapons = true;
		public List<string> DisabledWeaponDefNames = new List<string>();

		public void ResetToDefaults()
		{
			UseLoreDescriptions = true;
			UseGreyscaleTextures = false;
			EnableAllWeapons = true;
			DisabledWeaponDefNames.Clear();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref UseLoreDescriptions, "UseLoreDescriptions", true);
			Scribe_Values.Look(ref UseGreyscaleTextures, "UseGreyscaleTextures", false);
			Scribe_Values.Look(ref EnableAllWeapons, "EnableAllWeapons", true);
			Scribe_Collections.Look(ref DisabledWeaponDefNames, "DisabledWeaponDefNames", LookMode.Value);
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				DisabledWeaponDefNames ??= new List<string>();
			}
		}

		// helper to check whether a ThingDef is disabled according to settings
		public bool IsWeaponDisabled(ThingDef def)
		{
			if (def == null) return false;
			// if EnableAllWeapons is false, everything is disabled
			if (!EnableAllWeapons) return true;
			// otherwise disabled only if explicitly in the disabled list
			return DisabledWeaponDefNames != null && DisabledWeaponDefNames.Contains(def.defName);
		}
	}
}