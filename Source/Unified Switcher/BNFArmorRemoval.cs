using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BNF.StyleSwitcher
{
    public static class BNFArmorRemoval
    {
        /// <summary>
        /// Call this from BNFMod.DoSettingsWindowContents(listing) where you want the armor removal UI to appear.
        /// Example: BNFArmorRemoval.DrawRemovalSection(list, settings);
        /// </summary>
        public static void DrawRemovalSection(Listing_Standard listing, BNFSettings settings)
        {
            listing.Gap(8f);
            listing.Label("Disable specific armor groups (hides them from traders and generated spawns)");
            listing.Gap(4f);

            // Master toggle for enabling/disabling the whole feature
            listing.CheckboxLabeled("Enable armor removal", ref settings.EnableArmorRemoval);
            listing.Gap(6f);

            // Discover groups from ThingDefs that use the extension
            var groups = new Dictionary<string, string>(); // key -> label
            foreach (var def in DefDatabase<ThingDef>.AllDefs)
            {
                var ext = def.GetModExtension<BNFRemovableExtension>();
                if (ext == null) continue;
                string key = string.IsNullOrEmpty(ext.group) ? def.defName : ext.group;
                string label = string.IsNullOrEmpty(ext.label) ? key : ext.label;
                if (!groups.ContainsKey(key)) groups[key] = label;
            }

            if (groups.Count == 0)
            {
                listing.Label("No removable armor groups found in loaded defs.");
                return;
            }

            // Ensure settings contain each group
            foreach (var key in groups.Keys)
            {
                if (!settings.RemovedArmorGroups.ContainsKey(key))
                    settings.RemovedArmorGroups[key] = false;
            }

            // Disable the group checkboxes if the master toggle is off
            bool prevGui = GUI.enabled;
            GUI.enabled = settings.EnableArmorRemoval;

            // Render checkboxes
            foreach (var kv in groups)
            {
                string display = kv.Value + " (remove)";
                bool cur = settings.RemovedArmorGroups.TryGetValue(kv.Key, out var v) && v;
                listing.CheckboxLabeled(display, ref cur);
                settings.RemovedArmorGroups[kv.Key] = cur;
            }

            GUI.enabled = prevGui;

            listing.Gap(8f);
            // Save hint
            listing.Label("Toggle a group and press Save to persist. Re-enable to restore.");
        }
    }
}
