using Verse;

namespace BNF.StyleSwitcher
{
    /// <summary>
    /// Put this on ThingDefs to mark them as removable by the BNF settings.
    /// Use the group string to collect many defs under one checkbox (e.g. "medic", "support").
    /// </summary>
    public class BNFRemovableExtension : DefModExtension
    {
        public string group = "default";
        public string label = null!; // optional friendly label to show in settings
    }
}
