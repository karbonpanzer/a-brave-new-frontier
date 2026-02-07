using System.Collections.Generic;
using System.Xml;
using RimWorld;
using UnityEngine;
using VEF.Graphics;
using Verse;

namespace BNF.Graphics
{
    public class PawnRenderNodeProperties_OmniBNF : PawnRenderNodeProperties_Omni
    {
        // XML: <decalFolder>Symbols/Infantry</decalFolder>
        // Relative to Textures/, no extension.
        public string decalFolder = "Symbols";

        public Dictionary<BodyTypeDef, Vector3> bodyTypeOffsets = new Dictionary<BodyTypeDef, Vector3>();

        // Existing structure your worker uses
        public Dictionary<Rot4, Dictionary<BodyTypeDef, Vector3>> bodyTypeOffsetsByFacing =
            new Dictionary<Rot4, Dictionary<BodyTypeDef, Vector3>>();

        // Option 1: compact XML structure
        // Example:
        // <bodyTypeOffsetsByFacingRows>
        //   <li><bodyType>Fat</bodyType><north>(0,0,0.07)</north><south>(0,0,0.025)</south></li>
        // </bodyTypeOffsetsByFacingRows>
        public List<BodyTypeOffsetsByFacingRow> bodyTypeOffsetsByFacingRows;

        private bool offsetsBuilt;

        public void EnsureBodyTypeOffsetsByFacingBuilt()
        {
            if (offsetsBuilt) return;
            offsetsBuilt = true;

            if (bodyTypeOffsetsByFacing == null)
                bodyTypeOffsetsByFacing = new Dictionary<Rot4, Dictionary<BodyTypeDef, Vector3>>();

            if (bodyTypeOffsetsByFacingRows == null || bodyTypeOffsetsByFacingRows.Count == 0)
                return;

            for (int i = 0; i < bodyTypeOffsetsByFacingRows.Count; i++)
            {
                var row = bodyTypeOffsetsByFacingRows[i];
                if (row == null || row.bodyType == null) continue;

                if (row.hasNorth) Add(Rot4.North, row.bodyType, row.north);
                if (row.hasEast) Add(Rot4.East, row.bodyType, row.east);
                if (row.hasSouth) Add(Rot4.South, row.bodyType, row.south);
                if (row.hasWest) Add(Rot4.West, row.bodyType, row.west);
            }
        }

        private void Add(Rot4 rot, BodyTypeDef bodyType, Vector3 offset)
        {
            if (!bodyTypeOffsetsByFacing.TryGetValue(rot, out var dict) || dict == null)
            {
                dict = new Dictionary<BodyTypeDef, Vector3>();
                bodyTypeOffsetsByFacing[rot] = dict;
            }

            dict[bodyType] = offset;
        }
    }

    public class BodyTypeOffsetsByFacingRow
    {
        public BodyTypeDef bodyType;

        public Vector3 north;
        public Vector3 east;
        public Vector3 south;
        public Vector3 west;

        public bool hasNorth;
        public bool hasEast;
        public bool hasSouth;
        public bool hasWest;

        // Lets us detect presence/absence so omitting a facing means "no override"
        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(
                this, nameof(bodyType), xmlRoot["bodyType"]?.InnerText
            );

            ReadVec(xmlRoot, "north", ref north, ref hasNorth);
            ReadVec(xmlRoot, "east", ref east, ref hasEast);
            ReadVec(xmlRoot, "south", ref south, ref hasSouth);
            ReadVec(xmlRoot, "west", ref west, ref hasWest);
        }

        private static void ReadVec(XmlNode root, string nodeName, ref Vector3 vec, ref bool flag)
        {
            var node = root[nodeName];
            if (node == null) return;

            vec = ParseHelper.FromString<Vector3>(node.InnerText);
            flag = true;
        }
    }
}
