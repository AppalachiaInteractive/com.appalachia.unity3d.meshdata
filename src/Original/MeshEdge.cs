/*using System;

namespace Appalachia.Core.MeshData
{
    public struct MeshEdge : IEquatable<MeshEdge>
    {
        #region Constructor

        public MeshEdge(int aOriginalIndex, int bOriginalIndex)
        {
            this.aOriginalIndex = aOriginalIndex;
            this.bOriginalIndex = bOriginalIndex;
            triangleCount = 0;
        }

        public MeshEdge(int aOriginalIndex, int bOriginalIndex, int triangleCount)
        {
            this.aOriginalIndex = aOriginalIndex;
            this.bOriginalIndex = bOriginalIndex;
            this.triangleCount = triangleCount;
        }

        #endregion

        public readonly int aOriginalIndex;
        public readonly int bOriginalIndex;
        public readonly int triangleCount;

        #region IEquatable<MeshEdge>

        public bool Equals(MeshEdge other)
        {
            return (aOriginalIndex == other.aOriginalIndex && bOriginalIndex == other.bOriginalIndex) ||
                (aOriginalIndex == other.bOriginalIndex && bOriginalIndex == other.aOriginalIndex);
        }

        public override bool Equals(object obj)
        {
            return obj is MeshEdge other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (aOriginalIndex * 397) ^ bOriginalIndex;
                hashCode += (bOriginalIndex * 397) ^ aOriginalIndex;

                return hashCode;
            }
        }

        public static bool operator ==(MeshEdge left, MeshEdge right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MeshEdge left, MeshEdge right)
        {
            return !left.Equals(right);
        }

        #endregion
    }
}*/


