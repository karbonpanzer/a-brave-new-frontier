namespace BNF.StyleSwitcher
{
    internal static class PathUtil
    {
        public static string Normalize(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            path = path.Replace("\\", "/");
            // Strip any leading "Textures/" because RimWorld expects paths relative to the Textures root.
            if (path.StartsWith("Textures/")) path = path.Substring("Textures/".Length);
            if (path.StartsWith("Texture/")) path = path.Substring("Texture/".Length);
            return path.Trim();
        }
    }
}