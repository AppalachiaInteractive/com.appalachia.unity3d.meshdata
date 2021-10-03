/*
using System;
using System.Collections.Generic;
using System.Linq;
using Appalachia.Core.Profiling;
using Appalachia.Core.Terrains;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Appalachia.Core.MeshData
{
    
    
    [Serializable]
    public class MeshObject : IDisposable
    {
        public const float threshold = 1e-6f;
        
        [NonSerialized] private bool requiresRepopulation = true;

        public MeshVertex[] vertices;

        //public int[] vertexHashes;
        [NonSerialized] public List<MeshEdge> edges = new List<MeshEdge>();
        public MeshTriangle[] triangles;

        [NonSerialized] public List<MeshEdge> borderEdges = new List<MeshEdge>();
        [NonSerialized] public HashSet<MeshVertex> borderVertices = new HashSet<MeshVertex>();
        [NonSerialized] public MeshVertex[] borderVerticesArray;
        [NonSerialized] public Vector3 borderNormal;
        [NonSerialized] private List<MeshTriangle> _solidificationTriangles = new List<MeshTriangle>();

        //public Native native;
        
        public IEnumerable<MeshTriangle> solidTriangles
        {
            get
            {
                if (RequiresSolidification)
                {
                    CalculateSolidification();
                }
                
                return triangles.Concat(_solidificationTriangles);
            }
        }

        private Vector3[] _polyLine = new Vector3[4];

        private bool _borderChecked;

        public MeshObject(Mesh mesh, int groupingScale)
        {
            Populate(mesh, groupingScale);
        }

        public bool RequiresPopulation =>
            (vertices == null) || (vertices.Length == 0) || (triangles == null) || (triangles.Length == 0);

        public bool RequiresRepopulation =>
            RequiresPopulation ||
            requiresRepopulation ||
            (vertices[0].edges == null) ||

            //triangles[0].xy == null ||
            (edges == null) ||
            (edges.Count == 0);

        public bool RequiresBorderCheck =>
            RequiresRepopulation ||
            !_borderChecked ||
            borderEdges == null ||
            borderEdges.Count == 0 || 
            borderVerticesArray == null ||
            borderVerticesArray.Length == 0;

        public bool RequiresSolidification =>
            RequiresBorderCheck || (_solidificationTriangles == null) || (_solidificationTriangles.Count == 0);

        public void Populate(Mesh mesh, int groupingScale)
        {
           
using (ASPECT.Many(ASPECT.Profile(), ASPECT.Trace()))
            {
                var vertexIndices = ProcessVertices(mesh, groupingScale);

                ProcessTriangles(mesh, vertexIndices);

                RepopulateNonSerializedFields();

                CalculateBorderEdges();
            }
        }

        private Dictionary<short, short> ProcessVertices(Mesh mesh, int groupingScale)
        {
           
using (ASPECT.Many(ASPECT.Profile(), ASPECT.Trace()))
            {
                var verts = mesh.vertices;
                var normals = mesh.normals;

                var vertexHash = new Dictionary<MeshVertex.vkey, MeshVertex>(verts.Length);
                var vertexIndices = new Dictionary<short, short>(verts.Length);

                for (short i = 0; i < verts.Length; i++)
                {
                    var newVertex = new MeshVertex(verts[i], groupingScale);

                    var subvertex = new MeshSubvertex {index = i, normal = normals[i]};

                    newVertex.subvertices.Add(subvertex);

                    if (vertexHash.ContainsKey(newVertex.key))
                    {
                        newVertex = vertexHash[newVertex.key];
                        newVertex.subvertices.Add(subvertex);
                    }
                    else
                    {
                        vertexHash.Add(newVertex.key, newVertex);
                    }
                }

                vertices = new MeshVertex[vertexHash.Count];

                short index = 0;

                foreach (var vertex in vertexHash)
                {
                    var vert = vertex.Value;

                    vertices[index] = vertex.Value;

                    for (short j = 0; j < vert.subvertices.Count; j++)
                    {
                        var sub = vert.subvertices[j];

                        vertexIndices.Add(sub.index, index);
                    }

                    index += 1;
                }

                return vertexIndices;
            }
        }

        private void ProcessTriangles(Mesh mesh, Dictionary<short, short> vertexIndices)
        {
           
using (ASPECT.Many(ASPECT.Profile(), ASPECT.Trace()))
            {
                var tris = mesh.triangles;

               
using (ASPECT.Many(ASPECT.Profile(), ASPECT.Trace()))
                {
                    triangles = new MeshTriangle[tris.Length / 3];
                }

               
using (ASPECT.Many(ASPECT.Profile(), ASPECT.Trace()))
                {
                    for (var i = 0; i < tris.Length; i += 3)
                    {
                        var index = i / 3;

                        short originalXIndex;
                        short originalYIndex;
                        short originalZIndex;

                       
using (ASPECT.Many(ASPECT.Profile(), ASPECT.Trace()))
                        {
                            originalXIndex = (short) tris[i];
                            originalYIndex = (short) tris[i + 1];
                            originalZIndex = (short) tris[i + 2];
                        }

                       
using (ASPECT.Many(ASPECT.Profile(), ASPECT.Trace()))
                        {
                            triangles[index] = new MeshTriangle
                            {
                                index = (short) index,
                                originalXIndex = originalXIndex,
                                originalYIndex = originalYIndex,
                                originalZIndex = originalZIndex,
                                xIndex = vertexIndices[originalXIndex],
                                yIndex = vertexIndices[originalYIndex],
                                zIndex = vertexIndices[originalZIndex]

                                // xSubIndex = vertexSubindices[originalXIndex],
                                // ySubIndex = vertexSubindices[originalYIndex],
                                // zSubIndex = vertexSubindices[originalZIndex],
                            };
                        }
                    }
                }
            }
        }

        public void Refresh()
        {
           
using (ASPECT.Many(ASPECT.Profile(), ASPECT.Trace()))
            {
                if (RequiresRepopulation)
                {
                    RepopulateNonSerializedFields();
                }
            }
        }

        public void RepopulateNonSerializedFields()
        {
           
using (ASPECT.Many(ASPECT.Profile(), ASPECT.Trace()))
            {
                requiresRepopulation = false;

                if (edges == null)
                {
                    edges = new List<MeshEdge>();
                }

                edges.Clear();

                for (var i = 0; i < vertices.Length; i++)
                {
                    var vertex = vertices[i];
                    vertex.index = (short) i;

                    if (vertex.edges == null)
                    {
                        vertex.edges = new HashSet<MeshEdge>();
                    }

                    vertex.edges.Clear();
                }

                for (var i = 0; i < triangles.Length; i++)
                {
                    var triangle = triangles[i];

                    var x = vertices[triangle.xIndex];
                    var y = vertices[triangle.yIndex];
                    var z = vertices[triangle.zIndex];

                    RepopulateTriangleData(triangle, x, y, z);
                    RepopulateEdgeData(triangle, x, y, z);
                    RepopulateVertexData(triangle, x, y, z);
                }
            }
        }

        private void RepopulateTriangleData(MeshTriangle triangle, MeshVertex x, MeshVertex y, MeshVertex z)
        {
           
using (ASPECT.Many(ASPECT.Profile(), ASPECT.Trace()))
            {
                /*var subX = x.subvertices[triangle.xSubIndex];
                var subY = y.subvertices[triangle.ySubIndex];
                var subZ = z.subvertices[triangle.zSubIndex];#1#

                triangle.x = x;
                triangle.y = y;
                triangle.z = z;
                triangle.xIndex = x.index;
                triangle.yIndex = y.index;
                triangle.zIndex = z.index;

                /*triangle.subX = subX;
                triangle.subY = subY;
                triangle.subZ = subZ;#1#
            }
        }

        private void RepopulateEdgeData(MeshTriangle triangle, MeshVertex x, MeshVertex y, MeshVertex z)
        {
           
using (ASPECT.Many(ASPECT.Profile(), ASPECT.Trace()))
            {
                if (x.edges == null)
                {
                    x.edges = new HashSet<MeshEdge>();
                }

                if (y.edges == null)
                {
                    y.edges = new HashSet<MeshEdge>();
                }

                if (z.edges == null)
                {
                    z.edges = new HashSet<MeshEdge>();
                }

                var xy = GetEdge(x, y);
                var yz = GetEdge(y, z);
                var zx = GetEdge(z, x);

                if (xy == null)
                {
                    xy = new MeshEdge {a = x, b = y, index = (short) edges.Count};
                    edges.Add(xy);
                }

                if (yz == null)
                {
                    yz = new MeshEdge {a = y, b = z, index = (short) edges.Count};
                    edges.Add(yz);
                }

                if (zx == null)
                {
                    zx = new MeshEdge {a = z, b = x, index = (short) edges.Count};
                    edges.Add(zx);
                }

                xy.triangles.Add(triangle);
                yz.triangles.Add(triangle);
                zx.triangles.Add(triangle);

                triangle.xy = xy;
                triangle.yz = yz;
                triangle.zx = zx;
            }
        }

        private void RepopulateVertexData(MeshTriangle triangle, MeshVertex x, MeshVertex y, MeshVertex z)
        {
           
using (ASPECT.Many(ASPECT.Profile(), ASPECT.Trace()))
            {
                x.edges.Add(triangle.xy);
                x.edges.Add(triangle.zx);
                y.edges.Add(triangle.xy);
                y.edges.Add(triangle.yz);
                z.edges.Add(triangle.yz);
                z.edges.Add(triangle.zx);
            }
        }

        private MeshEdge GetEdge(MeshVertex a, MeshVertex b)
        {
           
using (ASPECT.Many(ASPECT.Profile(), ASPECT.Trace()))
            {
                if ((a.edges.Count > 0) && (b.edges.Count > 0))
                {
                    var matches = a.edges.FirstOrDefault(ae => b.edges.Contains(ae));

                    return matches;
                }

                return null;
            }
        }

        public void CalculateBorderEdges(bool force = false)
        {
           
using (ASPECT.Many(ASPECT.Profile(), ASPECT.Trace()))
            {
                if (borderEdges == null)
                {
                    borderEdges = new List<MeshEdge>();
                }

                if (borderVertices == null)
                {
                    borderVertices = new HashSet<MeshVertex>();
                }

                if (force || (borderEdges.Count == 0) || (borderVertices.Count == 0))
                {
                    borderEdges.Clear();
                    borderVertices.Clear();

                    var borderHash = new HashSet<MeshEdge>();

                    for (var i = 0; i < edges.Count; i++)
                    {
                        var edge = edges[i];

                        if (edge.triangles.Count == 1)
                        {
                            borderHash.Add(edge);
                        }
                    }

                    if (borderHash.Count > 500)
                    {
                        throw new NotSupportedException("Mesh has too many border vertices.");
                    }
                    
                    foreach (var edge in borderHash)
                    {
                        borderEdges.Add(edge);

                        borderVertices.Add(edge.a);
                        borderVertices.Add(edge.b);
                    }

                    borderVerticesArray = borderVertices.ToArray();

                    borderNormal = Vector3.zero;

                    for (var i = 0; i < borderVerticesArray.Length; i++)
                    for (var j = 0; j < borderVerticesArray.Length; j++)
                    for (var k = 0; k < borderVerticesArray.Length; k++)
                    {
                        if ((i == j) || (i == k) || (j == k))
                        {
                            continue;
                        }

                        var vi = borderVerticesArray[i].position;
                        var vj = borderVerticesArray[j].position;
                        var vk = borderVerticesArray[k].position;

                        var plane = new Plane(vi, vj, vk);

                        if (plane.normal.y < 0)
                        {
                            borderNormal -= plane.normal;
                        }
                        else
                        {
                            borderNormal += plane.normal;
                        }
                    }

                    borderNormal = math.normalizesafe(borderNormal);
                }
            }

            _borderChecked = true;
        }

        

        public void CalculateSolidification()
        {
            if (_solidificationTriangles == null)
            {
                _solidificationTriangles = new List<MeshTriangle>();
            }
            else
            {
                _solidificationTriangles.Clear();
            }

            var ordered = borderEdges.SelectMany(be => new[] {be.a, be.b}).ToArray();

            if (ordered.Length == 0) return;
            
            ReorderBorderVertices(ordered);

            var polygon = FindRealPolygon(ordered);
            
            if (polygon.Count == 0) return;

            int tri_forward = 0, tri_backward = polygon.Count - 1, tri_new = 1;
            var increment = true;

            while ((tri_new != tri_forward) && (tri_new != tri_backward))
            {
                var vert_backward = polygon[tri_backward];
                var vert_forward = polygon[tri_forward];
                var vert_new = polygon[tri_new];

                var newTriangle = new MeshTriangle()
                {
                    index = -1,
                    originalXIndex = vert_backward.index,
                    originalYIndex = vert_forward.index,
                    originalZIndex = vert_new.index,
                    xIndex = vert_backward.index,
                    yIndex = vert_forward.index,
                    zIndex = vert_new.index,
                    x = vert_backward,
                    y = vert_forward,
                    z = vert_new
                };

                _solidificationTriangles.Add(newTriangle);

                if (increment)
                {
                    tri_forward = tri_new;
                }
                else
                {
                    tri_backward = tri_new;
                }

                increment = !increment;
                tri_new = increment ? tri_forward + 1 : tri_backward - 1;
            }
        }

        public static void ReorderBorderVertices(MeshVertex[] pairs)
        {
            var nbFaces = 0;
            var faceStart = 0;
            var i = 0;

            while (i < pairs.Length)
            {
                // Find next adjacent edge
                for (var j = i + 2; j < pairs.Length; j += 2)
                {
                    if (pairs[j] == pairs[i + 1])
                    {
                        // Put j at i+2
                        SwitchPairs(pairs, i + 2, j);
                        break;
                    }
                }

                if ((i + 3) >= pairs.Length)
                {
                    // Why does this happen?
                    Debug.Log("Huh?");
                    break;
                }

                if (pairs[i + 3] == pairs[faceStart])
                {
                    // A face is complete.
                    nbFaces++;
                    i += 4;
                    faceStart = i;
                }
                else
                {
                    i += 2;
                }
            }
        }

        private static void SwitchPairs<T>(IList<T> pairs, int pos1, int pos2)
        {
            if (pos1 == pos2)
            {
                return;
            }

            var temp1 = pairs[pos1];
            var temp2 = pairs[pos1 + 1];
            pairs[pos1] = pairs[pos2];
            pairs[pos1 + 1] = pairs[pos2 + 1];
            pairs[pos2] = temp1;
            pairs[pos2 + 1] = temp2;
        }

        private static List<MeshVertex> FindRealPolygon(MeshVertex[] pairs)
        {
            var vertices = new List<MeshVertex>();

            for (var i = 0; i < pairs.Length; i += 2)
            {
                var edge1 = pairs[i + 1].position - pairs[i].position;
                Vector3 edge2;
                if (i == (pairs.Length - 2))
                {
                    edge2 = pairs[1].position - pairs[0].position;
                }
                else
                {
                    edge2 = pairs[i + 3].position - pairs[i + 2].position;
                }

                edge1.Normalize();
                edge2.Normalize();

                if (Vector3.Angle(edge1, edge2) > threshold)
                {
                    vertices.Add(pairs[i + 1]);
                }
            }

            return vertices;
        }

        public void DrawBorders(Matrix4x4 localToWorld, TerrainThreadsafeData terrain)
        {
           
using (ASPECT.Many(ASPECT.Profile(), ASPECT.Trace()))
            {
                CalculateBorderEdges();

                var handleColor = UnityEditor.Handles.color;
                var shift = .005f;

                for (var i = 0; i < borderEdges.Count; i++)
                {
                    var edge = borderEdges[i];

                    CalculateEdgeHeightDifference(
                        localToWorld,
                        terrain,
                        edge,
                        out var posA,
                        out var diffA,
                        out var posB,
                        out var diffB
                    );

                    UnityEditor.Handles.color = (diffA < 0) && (diffB < 0) ? Color.green : Color.red;

                    for (var j = 0; j < 3; j++)
                    {
                        var offset = Vector3.down * (shift * j);

                        UnityEditor.Handles.DrawLine(posA + offset, posB + offset);
                    }
                }

                var basePoint = localToWorld.MultiplyPoint(Vector3.zero);
                var normal = localToWorld.MultiplyVector(borderNormal);

                UnityEditor.Handles.DrawLine(basePoint, basePoint + normal);

                var terrainNormal = TerrainJobHelper.GetTerrainNormal(
                    basePoint,
                    terrain.terrainPosition,
                    terrain.heights,
                    terrain.resolution,
                    terrain.scale
                );

                UnityEditor.Handles.color = Color.cyan;

                UnityEditor.Handles.DrawLine(basePoint, basePoint + (Vector3) terrainNormal);

                UnityEditor.Handles.color = handleColor;
            }
        }

        public void DrawTerrain(Matrix4x4 localToWorld, TerrainThreadsafeData terrain)
        {
           
using (ASPECT.Many(ASPECT.Profile(), ASPECT.Trace()))
            {
                var t = terrain.GetTerrain();
                CalculateBorderEdges();

                var handleColor = UnityEditor.Handles.color;

                for (var i = 0; i < borderEdges.Count; i++)
                {
                    var edge = borderEdges[i];

                    CalculateEdgeHeightDifference(
                        localToWorld,
                        terrain,
                        edge,
                        out var posA,
                        out _,
                        out var posB,
                        out _
                    );

                    var posAT = posA;
                    var posBT = posB;

                    posAT.y = t.GetWorldHeightAtPosition(posA);
                    posBT.y = t.GetWorldHeightAtPosition(posB);

                    UnityEditor.Handles.color = Color.cyan;
                    UnityEditor.Handles.DrawLine(posAT, posBT);

                    var offsetA = (i % 2) == 0 ? .002f : -.002f;
                    var offsetB = -offsetA;

                    posAT.y = terrain.GetWorldSpaceHeight(posA) + offsetA;
                    posBT.y = terrain.GetWorldSpaceHeight(posB) + offsetB;

                    UnityEditor.Handles.color = Color.red;
                    UnityEditor.Handles.DrawLine(posAT, posBT);
                }

                UnityEditor.Handles.color = handleColor;
            }
        }

        public void DrawVertexStatus(Matrix4x4 localToWorld, TerrainThreadsafeData terrainThreadsafeData)
        {
           
using (ASPECT.Many(ASPECT.Profile(), ASPECT.Trace()))
            {
                var handleColor = UnityEditor.Handles.color;

                for (var i = 0; i < vertices.Length; i += 20)
                {
                    var vertex = vertices[i];

                    var pos = localToWorld.MultiplyPoint(vertex.position);

                    var diff = TerrainJobHelper.CalculateHeightDifference(
                        pos,
                        terrainThreadsafeData.terrainPosition,
                        terrainThreadsafeData.heights,
                        terrainThreadsafeData.resolution,
                        terrainThreadsafeData.scale
                    );

                    UnityEditor.Handles.color = diff < 0 ? Color.red : Color.green;

                    var normal = (i % 3) == 0
                        ? Vector3.up
                        : (i % 3) == 1
                            ? Vector3.right
                            : Vector3.forward;

                    UnityEditor.Handles.DrawWireDisc(pos, normal, .005f);
                }

                UnityEditor.Handles.color = handleColor;
            }
        }

        public void DrawTriangle(Matrix4x4 localToWorld, int triangleIndex, Color color)
        {
           
using (ASPECT.Many(ASPECT.Profile(), ASPECT.Trace()))
            {
                var handleColor = UnityEditor.Handles.color;
                UnityEditor.Handles.color = color;

                var triangle = triangles[triangleIndex];

                var vertexX = vertices[triangle.xIndex];
                var vertexY = vertices[triangle.yIndex];
                var vertexZ = vertices[triangle.zIndex];

                var x = localToWorld.MultiplyPoint(vertexX.position);
                var y = localToWorld.MultiplyPoint(vertexY.position);
                var z = localToWorld.MultiplyPoint(vertexZ.position);

                if (_polyLine == null)
                {
                    _polyLine = new Vector3[4];
                }

                _polyLine[0] = x;
                _polyLine[1] = y;
                _polyLine[2] = z;
                _polyLine[3] = x;

                UnityEditor.Handles.DrawPolyLine(_polyLine);
                UnityEditor.Handles.color = handleColor;
            }
        }

        private void CalculateEdgeHeightDifference(
            Matrix4x4 localToWorld,
            TerrainThreadsafeData terrainThreadsafeData,
            MeshEdge edge,
            out Vector3 posA,
            out float diffA,
            out Vector3 posB,
            out float diffB)
        {
            var vertexA = vertices[edge.a.index];
            var vertexB = vertices[edge.b.index];

            posA = localToWorld.MultiplyPoint(vertexA.position);
            posB = localToWorld.MultiplyPoint(vertexB.position);

            diffA = TerrainJobHelper.CalculateHeightDifference(
                posA,
                terrainThreadsafeData.terrainPosition,
                terrainThreadsafeData.heights,
                terrainThreadsafeData.resolution,
                terrainThreadsafeData.scale
            );

            diffB = TerrainJobHelper.CalculateHeightDifference(
                posA,
                terrainThreadsafeData.terrainPosition,
                terrainThreadsafeData.heights,
                terrainThreadsafeData.resolution,
                terrainThreadsafeData.scale
            );
        }
        
        public void Dispose()
        {
        }

    }
}
*/


