using System.Collections.Generic;
using System.Xml.Serialization;

namespace Lootroom.Models
{
    public class LootStorage
    {
        public ushort StorageID;
        [XmlArray("ItemsIDS")]
        [XmlArrayItem("ItemID")]
        public List<List<ushort>> LootOptions;
        public SerializableVector3 StoragePosition;
        public float AngleX;
        public float AngleY;
        public float AngleZ;
    }
}