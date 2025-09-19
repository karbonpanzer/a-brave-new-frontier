using System.Collections.Generic;
using Verse;

namespace BNF.StyleSwitcher
{
    public class BNFSettings : ModSettings
    {
        // existing fields (keep your text/texture switches)
        public bool EnableDescriptionSwitch = true;
        public bool UseLoreDescriptions = true;
        public bool EnableTextureSwitch = true;
        public bool UseGreyscaleTextures = false;

        // New: which removable groups are disabled (true = removed)
        public Dictionary<string, bool> RemovedArmorGroups = new Dictionary<string, bool>();

        // Master toggle to enable/disable the armor removal feature entirely
        public bool EnableArmorRemoval = false;

        public void ResetToDefaults()
        {
            EnableDescriptionSwitch = true;
            UseLoreDescriptions = true;
            EnableTextureSwitch = true;
            UseGreyscaleTextures = false;

            RemovedArmorGroups.Clear();
            EnableArmorRemoval = false;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref EnableDescriptionSwitch, "EnableDescriptionSwitch", true);
            Scribe_Values.Look(ref UseLoreDescriptions, "UseLoreDescriptions", true);
            Scribe_Values.Look(ref EnableTextureSwitch, "EnableTextureSwitch", true);
            Scribe_Values.Look(ref UseGreyscaleTextures, "UseGreyscaleTextures", false);

            Scribe_Values.Look(ref EnableArmorRemoval, "EnableArmorRemoval", false);

            // Persist RemovedArmorGroups as two parallel lists
            List<string> keys = new List<string>();
            List<bool> vals = new List<bool>();
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                keys.AddRange(RemovedArmorGroups.Keys);
                foreach (var k in keys) vals.Add(RemovedArmorGroups[k]);
            }

            Scribe_Collections.Look(ref keys, "RemovedArmorKeys", LookMode.Value);
            Scribe_Collections.Look(ref vals, "RemovedArmorVals", LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                RemovedArmorGroups.Clear();
                if (keys != null && vals != null && keys.Count == vals.Count)
                {
                    for (int i = 0; i < keys.Count; i++)
                        RemovedArmorGroups[keys[i]] = vals[i];
                }
            }
        }
    }
}
