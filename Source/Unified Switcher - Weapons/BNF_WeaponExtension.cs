using Verse;

namespace BNF.StyleSwitcher
{
	// Marker extension to identify BNF weapon ThingDefs.
	// Add to ThingDefs via modExtensions with Class="BNF.StyleSwitcher.BNFWeaponExtension"
	public class BNFWeaponExtension : DefModExtension
	{
		// Optional friendly name shown in the settings UI. Falls back to the ThingDef label if empty.
		public string displayName;

		// If true, UI can hide this weapon when disabled (not automatic).
		public bool hideWhenDisabled = false;

		// Optional replacement defName string to swap in when disabled, if you want to use a placeholder.
		public string replacementDefName;
	}
}