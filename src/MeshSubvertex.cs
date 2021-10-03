#region

using System;
using Appalachia.Core.HashKeys;
using Unity.Mathematics;

#endregion

namespace Appalachia.Core.MeshData
{
    public struct MeshSubvertex : IEquatable<MeshSubvertex>
    {
#region Constructor

        public MeshSubvertex(float3 normal, int groupingScale, int originalIndex)
        {
            this.normal = normal;
            key = new JobFloat3Key(normal, groupingScale);
            this.originalIndex = originalIndex;
        }

#endregion

        public readonly JobFloat3Key key;
        public readonly float3 normal;
        public readonly int originalIndex;

#region IEquatable<MeshSubvertex>

        public bool Equals(MeshSubvertex other)
        {
            return key.Equals(other.key);
        }

        public override bool Equals(object obj)
        {
            return obj is MeshSubvertex other && Equals(other);
        }

        public override int GetHashCode()
        {
            return key.GetHashCode();
        }

        public static bool operator ==(MeshSubvertex left, MeshSubvertex right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MeshSubvertex left, MeshSubvertex right)
        {
            return !left.Equals(right);
        }

#endregion
    }
}
