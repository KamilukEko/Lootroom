using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;

namespace Lootroom.Models
{
    public class SerializableVector3
    {
        [XmlAttribute]
        public float X, Y, Z;

        private Vector3? _vector3;
        public Vector3 Vector3
        {
            get
            {
                if (_vector3.HasValue) return _vector3.Value;
                _vector3 = new Vector3(X, Y, Z);
                return _vector3.Value;
            }
        }
        
        public SerializableVector3(Vector3 vector)
        {
            X = vector.x;
            Y = vector.y;
            Z = vector.z;
        }

        public SerializableVector3()
        {
            X = 0;
            Y = 0;
            Z = 0;
        }

        public SerializableVector3(string vector)
        {
            var arr = vector.Split('|').Select(float.Parse).ToList();
            
            X = arr[0];
            Y = arr[1];
            Z = arr[2];
        }

        public SerializableVector3(byte[] bytes)
        {
            X = BitConverter.ToSingle(bytes, 0);
            Y = BitConverter.ToSingle(bytes, 4);
            Z = BitConverter.ToSingle(bytes, 8);
        }

        public override string ToString()
        {
            return $"{X}|{Y}|{Z}";
        }

        public byte[] ToBytes()
        {
            var output = new List<byte>();
            output.AddRange(BitConverter.GetBytes(X));
            output.AddRange(BitConverter.GetBytes(Y));
            output.AddRange(BitConverter.GetBytes(Z));
            
            return output.ToArray();
        }
    }
}
