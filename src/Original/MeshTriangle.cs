/*using System;
using System.Collections.Generic;
using Appalachia.Core.Profiling;

namespace Appalachia.Core.MeshData
{
    [Serializable]
    public class MeshTriangle : IEquatable<MeshTriangle>
    {
        public short index;


        //public short zSubIndex;

        public short originalXIndex;
        public short originalYIndex;
        public short originalZIndex;
        

        public short xIndex;
        public short yIndex;
        public short zIndex;

        [NonSerialized] public MeshVertex x;
        [NonSerialized] public MeshVertex y;
        [NonSerialized] public MeshVertex z;
        
        [NonSerialized] public MeshSubvertex subX;
        [NonSerialized] public MeshSubvertex subY;
        [NonSerialized] public MeshSubvertex subZ;

        [NonSerialized] public MeshEdge xy;
        [NonSerialized] public MeshEdge yz;
        [NonSerialized] public MeshEdge zx;

        public void AddToTriangleList(IList<int> triangles)
        {
            triangles.Add(x.index);
            triangles.Add(y.index);
            triangles.Add(z.index);
        }

        private int[] _vertexArray;
        
        public int[] GetVertexArray()
        {
            if (_vertexArray == null || _vertexArray.Length != 3)
            {
                _vertexArray = new int[3];
            }
            
            if (_vertexArray[0] == _vertexArray[1])
            {
                _vertexArray[0] = x.index;
                _vertexArray[1] = y.index;
                _vertexArray[2] = z.index;
            }

            return _vertexArray;
        }
        
        #region IEquatable<MeshTriangle>

        public bool Equals(MeshTriangle other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return index == other.index &&
                xIndex == other.xIndex && /*xSubIndex == other.xSubIndex &&#1#
                yIndex == other.yIndex && /*ySubIndex == other.ySubIndex &&#1#
                zIndex == other.zIndex && /*zSubIndex == other.zSubIndex &&#1#
                originalXIndex == other.originalXIndex &&
                originalYIndex == other.originalYIndex &&
                originalZIndex == other.originalZIndex;
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

            return Equals((MeshTriangle) obj);
        }

        public override int GetHashCode()
        {
           
using (ASPECT.Many(ASPECT.Profile(), ASPECT.Trace()))
            {
                unchecked
                {
                    var hashCode = (int)index;
                    hashCode = (hashCode * 397) ^ xIndex;

                    //hashCode = (hashCode * 397) ^ xSubIndex;
                    hashCode = (hashCode * 397) ^ yIndex;

                    //hashCode = (hashCode * 397) ^ ySubIndex;
                    hashCode = (hashCode * 397) ^ zIndex;

                    //hashCode = (hashCode * 397) ^ zSubIndex;
                    hashCode = (hashCode * 397) ^ originalXIndex;
                    hashCode = (hashCode * 397) ^ originalYIndex;
                    hashCode = (hashCode * 397) ^ originalZIndex;
                    return hashCode;
                }
            }
        }

        public static bool operator ==(MeshTriangle left, MeshTriangle right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(MeshTriangle left, MeshTriangle right)
        {
            return !Equals(left, right);
        }

        #endregion
        
    }
}*/


