using UnityEngine;
using Verse;

namespace BNF.Core.DecalSystem
{
    public sealed class DecalSymbolDef : Def
    {
        public string Path = "";
    }

    public struct DecalProfile
    {
        public bool Active;
        public string SymbolPath;
        public Color SymbolColor;

        public DecalProfile(bool active, string path, Color color)
        {
            Active = active;
            SymbolPath = path;
            SymbolColor = color;
        }

        public static DecalProfile Default => new DecalProfile(false, "", Color.white);
    }
}