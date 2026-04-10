using UnityEngine;
using Verse;

namespace BNF.Core.DecalSystem
{
    //This is to setup for the symboldefs, basically using a similar structure VFEM2. It is shared between Helmet/Armor because they are scaled and offset independently
    public sealed class DecalSymbolDef : Def
    {
        public string Path = "";
    }

    public enum DecalSlot { Helmet, Armor }

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

    public struct DecalProfileSet
    {
        public DecalProfile Helmet;
        public DecalProfile Armor;

        public DecalProfileSet(DecalProfile helmet, DecalProfile armor)
        {
            Helmet = helmet;
            Armor = armor;
        }

        public static DecalProfileSet Default => new DecalProfileSet(DecalProfile.Default, DecalProfile.Default);
    }
}