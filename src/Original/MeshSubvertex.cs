/*using System;
using UnityEngine;

namespace Appalachia.Core.MeshData
{
    [Serializable]
    public class MeshSubvertex : IEquatable<MeshSubvertex>
    {
        public Vector3 normal;

        public short index;

        public bool Equals(MeshSubvertex other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return normal.Equals(other.normal) && index == other.index;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((MeshSubvertex) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (normal.GetHashCode() * 397) ^ (int)index;
            }
        }

        public static bool operator ==(MeshSubvertex left, MeshSubvertex right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(MeshSubvertex left, MeshSubvertex right)
        {
            return !Equals(left, right);
        }
    }
}*/


