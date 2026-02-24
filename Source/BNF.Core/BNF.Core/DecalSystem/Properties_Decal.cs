using System.Collections.Generic;
using System.Xml;
using RimWorld;
using UnityEngine;
using Verse;

namespace BNF.Core.DecalSystem
{
    public class PawnRenderNodePropertiesOmniBnf : PawnRenderNodeProperties
    {
        public new List<string> texPaths = new List<string>(); 
        
        public new Color color = new Color(0.2f, 0.2f, 0.2f); 
        
        public readonly Dictionary<BodyTypeDef, Vector3> BodyTypeOffsets = new Dictionary<BodyTypeDef, Vector3>();
        public readonly Dictionary<Rot4, Dictionary<BodyTypeDef, Vector3>> BodyTypeOffsetsByFacing = new Dictionary<Rot4, Dictionary<BodyTypeDef, Vector3>>();
        public List<BodyTypeOffsetsByFacingRow> BodyTypeOffsetsByFacingRows = new List<BodyTypeOffsetsByFacingRow>();

        public PawnRenderNodePropertiesOmniBnf()
        {
            nodeClass = typeof(PawnRenderNodeDecal);
            workerClass = typeof(PawnRenderNodeWorkerOmniBodyApparelBnf);
        }

        public void EnsureBodyTypeOffsetsByFacingBuilt()
        {
            if (BodyTypeOffsetsByFacing.Count > 0 || BodyTypeOffsetsByFacingRows == null) return;
            foreach (var row in BodyTypeOffsetsByFacingRows)
            {
                if (row.BodyType == null) continue;
                if (row.HasNorth) Add(Rot4.North, row.BodyType, row.North);
                if (row.HasEast) Add(Rot4.East, row.BodyType, row.East);
                if (row.HasSouth) Add(Rot4.South, row.BodyType, row.South);
                if (row.HasWest) Add(Rot4.West, row.BodyType, row.West);
                
                if (row.HasOffset) BodyTypeOffsets[row.BodyType] = row.Offset;
                else if (row.HasSouth) BodyTypeOffsets[row.BodyType] = row.South;
            }
        }

        private void Add(Rot4 rot, BodyTypeDef bodyType, Vector3 offset)
        {
            if (!BodyTypeOffsetsByFacing.TryGetValue(rot, out var dict))
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
        public Vector3 Offset, North, East, South, West;
        public bool HasOffset, HasNorth, HasEast, HasSouth, HasWest;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, nameof(BodyType), xmlRoot["bodyType"]?.InnerText);
            ReadVec(xmlRoot, "offset", ref Offset, ref HasOffset);
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