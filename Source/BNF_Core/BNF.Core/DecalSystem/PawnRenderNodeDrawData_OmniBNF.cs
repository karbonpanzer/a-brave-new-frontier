using System.Collections.Generic;
using System.Xml;
using RimWorld;
using UnityEngine;
using VEF.Graphics;
using Verse;

namespace BNF.Core.DecalSystem
{
    public class PawnRenderNodePropertiesOmniBnf : PawnRenderNodeProperties_Omni
    {
        public readonly Dictionary<BodyTypeDef, Vector3> BodyTypeOffsets = new Dictionary<BodyTypeDef, Vector3>();

        public readonly Dictionary<Rot4, Dictionary<BodyTypeDef, Vector3>> BodyTypeOffsetsByFacing =
            new Dictionary<Rot4, Dictionary<BodyTypeDef, Vector3>>();

        public List<BodyTypeOffsetsByFacingRow>? BodyTypeOffsetsByFacingRows;

        private bool _offsetsBuilt;

        public void EnsureBodyTypeOffsetsByFacingBuilt()
        {
            if (_offsetsBuilt) return;
            _offsetsBuilt = true;

            var rows = BodyTypeOffsetsByFacingRows;
            if (rows == null || rows.Count == 0) return;

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row?.BodyType == null) continue;

                if (row.HasNorth) Add(Rot4.North, row.BodyType, row.North);
                if (row.HasEast) Add(Rot4.East, row.BodyType, row.East);
                if (row.HasSouth) Add(Rot4.South, row.BodyType, row.South);
                if (row.HasWest) Add(Rot4.West, row.BodyType, row.West);
            }
        }

        private void Add(Rot4 rot, BodyTypeDef bodyType, Vector3 offset)
        {
            if (!BodyTypeOffsetsByFacing.TryGetValue(rot, out var dict) || dict == null)
            {
                dict = new Dictionary<BodyTypeDef, Vector3>();
                BodyTypeOffsetsByFacing[rot] = dict;
            }

            dict[bodyType] = offset;
        }
    }

    public class BodyTypeOffsetsByFacingRow
    {
        public BodyTypeDef? BodyType;

        public Vector3 North;
        public Vector3 East;
        public Vector3 South;
        public Vector3 West;

        public bool HasNorth;
        public bool HasEast;
        public bool HasSouth;
        public bool HasWest;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(
                this, nameof(BodyType), xmlRoot["bodyType"]?.InnerText
            );

            ReadVec(xmlRoot, "north", ref North, ref HasNorth);
            ReadVec(xmlRoot, "east", ref East, ref HasEast);
            ReadVec(xmlRoot, "south", ref South, ref HasSouth);
            ReadVec(xmlRoot, "west", ref West, ref HasWest);
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
