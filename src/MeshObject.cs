#region

using System;
using System.Collections.Generic;
using Appalachia.Core.Collections.Native;
using Appalachia.Core.Extensions;
using Appalachia.Jobs.Burstable;
using Appalachia.Jobs.Types.HashKeys;
using Appalachia.Utility.Constants;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

#endregion

namespace Appalachia.MeshData
{
    public struct MeshObject : IDisposable
    {
        private const int LOOP = 128;

        public bool isCreated;

        public NativeList<MeshVertex> vertices;
        public NativeList<double> vertexPoints;
        public NativeList<float3> vertexPositions;
        public NativeArray<MeshSubvertex> subvertices;
        public NativeArray<int> originalToNewVertexMapping;
        public NativeList<MeshEdge> edges;

        public NativeArray<MeshTriangle> triangles;
        public NativeArray<float> triangleSurfaceAreas;
        public NativeArray<float3> triangleFaceNormals;
        public NativeArray<float3> triangleFaceMidpoints;
        public NativeList<int> triangleIndices;

        private NativeArray<int> borderEdgeCount;
        public NativeList<float3> borderEdgeNormals;
        public NativeList<int> borderEdgeIndices;

        private NativeArray<float3> averageFaceNormal_singleitem;
        private NativeArray<float3> borderEdgeNormal_singleitem;

        private NativeArray<BoundsBurst> boundsData_singleitem;
        private NativeArray<float> volume_singleitem;
        private NativeArray<float3> centerOfMass_singleitem;
        private NativeArray<float> surfaceArea_singleitem;

        public BoundsBurst Bounds => boundsData_singleitem[0];
        public float Volume => volume_singleitem[0];
        public float3 CenterOfMass => centerOfMass_singleitem[0];
        public float SurfaceArea => surfaceArea_singleitem[0];
        public float3 BorderNormal => borderEdgeNormal_singleitem[0];
        public float3 AverageFaceNormal => averageFaceNormal_singleitem[0];

        public bool isSolidified;
        public bool IsSolid => borderEdgeCount.Length == 0;

        public NativeList<MeshTriangle> solidTriangles;
        public NativeList<float> solidTriangleSurfaceAreas;
        public NativeList<float3> solidTriangleFaceNormals;
        public NativeList<float3> solidTriangleFaceMidpoints;
        public NativeList<int> solidTriangleIndices;

        private NativeArray<float> solidVolume_singleitem;
        private NativeArray<float3> solidCenterOfMass_singleitem;
        private NativeArray<float> solidSurfaceArea_singleitem;
        public float SolidVolume => solidVolume_singleitem[0];
        public float3 SolidCenterOfMass => solidCenterOfMass_singleitem[0];
        public float SolidSurfaceArea => solidSurfaceArea_singleitem[0];

        public MeshObject(Mesh mesh, bool solidify, JobHandle deps = default) : this(mesh.vertices, mesh.normals, mesh.triangles, solidify, deps)
        {
        }

        public static MeshObject Bounded(
            float3[] verts,
            Vector3[] normals,
            int[] tris,
            Bounds bounds,
            bool solidify,
            JobHandle deps = default)
        {
            var inBounds = new HashSet<int>();

            var vertLength = verts.Length;

            for (var i = 0; i < vertLength; i++)
            {
                var vert = verts[i];

                if (bounds.Contains(vert))
                {
                    inBounds.Add(i);
                }
            }

            var requiredIndices = new HashSet<int>();

            var originalTriLength = tris.Length;
            for (var i = 0; i < originalTriLength; i += 3)
            {
                var x = tris[i];
                var y = tris[i + 1];
                var z = tris[i + 2];

                if (inBounds.Contains(x) || inBounds.Contains(y) || inBounds.Contains(z))
                {
                    requiredIndices.Add(x);
                    requiredIndices.Add(y);
                    requiredIndices.Add(z);
                }
            }

            var updatedIndices = new int[verts.Length];
            var newVertices = new Vector3[requiredIndices.Count];
            var newNormals = new Vector3[requiredIndices.Count];

            var currentVertIndex = 0;

            for (var i = 0; i < vertLength; i++)
            {
                if (requiredIndices.Contains(i))
                {
                    updatedIndices[i] = currentVertIndex;
                    newVertices[currentVertIndex] = verts[i];
                    newNormals[currentVertIndex] = normals[i];

                    currentVertIndex += 1;
                }
            }

            var newTriangles = new List<int>();

            for (var i = 0; i < tris.Length; i += 3)
            {
                var x = tris[i];
                var y = tris[i + 1];
                var z = tris[i + 2];

                if (requiredIndices.Contains(x) || requiredIndices.Contains(y) || requiredIndices.Contains(z))
                {
                    var newX = updatedIndices[x];
                    var newY = updatedIndices[y];
                    var newZ = updatedIndices[z];

                    newTriangles.Add(newX);
                    newTriangles.Add(newY);
                    newTriangles.Add(newZ);
                }
            }

            return new MeshObject(newVertices, newNormals, newTriangles.ToArray(), solidify, deps);
        }

        public MeshObject(Vector3[] verts, Vector3[] normals, int[] tris, bool solidify, JobHandle deps = default)
        {
            var triangleCount = tris.Length / 3;

            vertices = new NativeList<MeshVertex>(verts.Length, Allocator.Persistent);
            vertexPositions = new NativeList<float3>(verts.Length, Allocator.Persistent);
            vertexPoints = new NativeList<double>(3 * verts.Length, Allocator.Persistent);
            subvertices = new NativeArray<MeshSubvertex>(verts.Length, Allocator.Persistent);
            originalToNewVertexMapping = new NativeArray<int>(verts.Length, Allocator.Persistent);
            edges = new NativeList<MeshEdge>(triangleCount, Allocator.Persistent);
            triangles = new NativeArray<MeshTriangle>(triangleCount, Allocator.Persistent);
            triangleSurfaceAreas = new NativeArray<float>(triangleCount, Allocator.Persistent);
            triangleFaceNormals = new NativeArray<float3>(triangleCount, Allocator.Persistent);
            triangleFaceMidpoints = new NativeArray<float3>(triangleCount, Allocator.Persistent);
            triangleIndices = new NativeList<int>(tris.Length, Allocator.Persistent);

            averageFaceNormal_singleitem = new NativeArray<float3>(1, Allocator.Persistent);
            borderEdgeCount = new NativeArray<int>(1, Allocator.Persistent);
            borderEdgeNormal_singleitem = new NativeArray<float3>(1, Allocator.Persistent);
            boundsData_singleitem = new NativeArray<BoundsBurst>(1, Allocator.Persistent);
            volume_singleitem = new NativeArray<float>(1, Allocator.Persistent);
            centerOfMass_singleitem = new NativeArray<float3>(1, Allocator.Persistent);
            surfaceArea_singleitem = new  NativeArray<float>(1, Allocator.Persistent);
            borderEdgeIndices = new NativeList<int>(tris.Length, Allocator.Persistent);
            borderEdgeNormals = new NativeList<float3>(tris.Length, Allocator.Persistent);

            var allEdges = new NativeList<int2>(tris.Length, Allocator.TempJob);
            var vertexHash = new NativeHashMap<JobFloat3Key, int>(verts.Length, Allocator.TempJob);
            var originalVertexNormals = new NativeArray<Vector3>(normals, Allocator.TempJob);
            var originalVertexPositions = new NativeArray<Vector3>(verts, Allocator.TempJob);
            var originalTriangleIndices = new NativeArray<int>(tris, Allocator.TempJob);
            var edgeHash = new NativeHashMap<MeshEdge, int>(verts.Length, Allocator.TempJob);
            var borderEdgeIndexHash = new Core.Collections.Native.NativeHashSet<int2>(tris.Length * 2, Allocator.TempJob);

            var volumes = new NativeList<float>(tris.Length, Allocator.TempJob);
            var centersOfMass = new NativeList<float3>(tris.Length, Allocator.TempJob);

            JobHandle handle;

            handle = new InitializeVertexJobs
            {
                /* R  */
                verts = originalVertexPositions,
                normals = originalVertexNormals,
                /* RW */
                vertexHash = vertexHash,
                vertices = vertices,
                /*  W */
                vertexPositions = vertexPositions,
                vertexPoints = vertexPoints,
                subvertices = subvertices,
                originalToNewVertexMapping = originalToNewVertexMapping,
                boundsData = boundsData_singleitem
            }.Schedule(deps);

            handle = new PopulateTriangleDataJob_PF
            {
                /* R  */
                tris = originalTriangleIndices,
                originalToNewVertexMapping = originalToNewVertexMapping,

                /*  W */
                triangles = triangles,
                allEdges = allEdges.AsParallelWriter()
            }.Schedule(tris.Length, LOOP, handle);

            handle = new CountEdgesJob
            {
                /* R  */
                allEdges = allEdges.AsDeferredJobArray(),
                /* RW */
                edgeCounts = edgeHash,
                /*  W */
                edges = edges
            }.Schedule(handle);

            handle = new RecreateEdgesJob
            {
                /* R  */
                edgeTriangleCounts = edgeHash,
                /*  W */
                edges = edges.AsDeferredJobArray(),
                borderEdgeCount = borderEdgeCount
            }.Schedule(handle);

            handle = new GetBorderEdgeIndicesJob_PF
            {
                /* R  */
                edges = edges.AsDeferredJobArray(),
                vertices = vertices.AsDeferredJobArray(),
                /*  W */
                borderEdgeIndices = borderEdgeIndices.AsParallelWriter(),
                borderEdgeIndexHash = borderEdgeIndexHash.AsParallelWriter()
            }.Schedule(edges, LOOP, handle);

            handle = new CalculateSurfaceDataJob
            {
                vertices = vertices.AsDeferredJobArray(),
                triangles = triangles,
                triangleSurfaceAreas = triangleSurfaceAreas,
                triangleFaceNormals = triangleFaceNormals,
                triangleFaceMidpoints = triangleFaceMidpoints,
                surfaceArea = surfaceArea_singleitem
            }.Schedule(handle);

            handle = new CalculateFaceNormal {subvertices = subvertices, faceNormal = averageFaceNormal_singleitem}.Schedule(handle);

            handle = new TrimBorderEdgeNormalsJob {borderEdgeIndices = borderEdgeIndices.AsDeferredJobArray(), borderEdgeNormals = borderEdgeNormals}
               .Schedule(handle);

            handle = new CalculateBorderNormalJob_PF
            {
                /* R  */
                vertices = vertices.AsDeferredJobArray(),
                /* R  */
                borderEdgeIndices = borderEdgeIndices.AsDeferredJobArray(),
                /* R  */
                edges = edges.AsDeferredJobArray(),
                /*  W */
                borderEdgeNormals = borderEdgeNormals.AsDeferredJobArray()
            }.Schedule(borderEdgeIndices, LOOP, handle);

            handle = new NormalizeBorderNormalJob
            {
                /* R  */
                borderEdgeNormals = borderEdgeNormals.AsDeferredJobArray(),
                faceNormal = averageFaceNormal_singleitem,
                /*  W */
                borderEdgeNormal = borderEdgeNormal_singleitem // [WriteOnly] public NativeArray<float3> borderEdgeNormal;
            }.Schedule(handle);

            // ** break out if we are not requiring solid meshes
            if (!solidify)
            {
                isCreated = true;

                JobHandle.ScheduleBatchedJobs();
                
                handle.Complete();

                allEdges.Dispose();
                vertexHash.Dispose();
                originalVertexNormals.Dispose();
                originalVertexPositions.Dispose();
                originalTriangleIndices.Dispose();
                edgeHash.Dispose();
                borderEdgeIndexHash.Dispose();
                volumes.Dispose();
                centersOfMass.Dispose();

                solidCenterOfMass_singleitem = default;
                isSolidified = false;
                solidTriangles = default;
                solidTriangleIndices = default;
                solidTriangleSurfaceAreas = default;
                solidTriangleFaceNormals = default;
                solidTriangleFaceMidpoints = default;
                solidVolume_singleitem = default;
                solidSurfaceArea_singleitem = default;
                
                return;
            } // ** break out if we are not requiring solid meshes

            solidTriangles = new NativeList<MeshTriangle>(triangleCount * 2, Allocator.Persistent);
            solidTriangleSurfaceAreas = new NativeList<float>(triangleCount * 2, Allocator.Persistent);
            solidTriangleFaceNormals = new NativeList<float3>(triangleCount * 2, Allocator.Persistent);
            solidTriangleFaceMidpoints = new NativeList<float3>(triangleCount * 2, Allocator.Persistent);
            solidTriangleIndices = new NativeList<int>(tris.Length * 2, Allocator.Persistent);
            solidVolume_singleitem = new NativeArray<float>(1, Allocator.Persistent);
            solidCenterOfMass_singleitem = new NativeArray<float3>(1, Allocator.Persistent);
            solidSurfaceArea_singleitem = new NativeArray<float>(1, Allocator.Persistent);

            var solidVolumes = new NativeList<float>(tris.Length, Allocator.TempJob);
            var solidCentersOfMass = new NativeList<float3>(tris.Length, Allocator.TempJob);
            var borderVertexPairs = new NativeList<MeshVertex>(tris.Length, Allocator.TempJob);
            var polygonVertices = new NativeList<MeshVertex>(tris.Length,   Allocator.TempJob);
            var solidificationTriangles = new NativeList<MeshTriangle>(triangleCount, Allocator.TempJob);

            handle = new TrimBorderVertexPairsJob
            {
                borderEdgeIndices = borderEdgeIndices.AsDeferredJobArray(), borderVertexPairs = borderVertexPairs
            }.Schedule(handle);

            handle = new BuildBorderPolygonPairListJob_PF
            {
                /* R  */
                vertices = vertices.AsDeferredJobArray(),
                /* R  */
                borderEdgeIndices = borderEdgeIndices.AsDeferredJobArray(),
                /* R  */
                edges = edges.AsDeferredJobArray(),
                /*  W */
                borderVertexPairs = borderVertexPairs.AsDeferredJobArray()
            }.Schedule(borderEdgeIndices, LOOP, handle);

            handle = new ReorderBorderPolygonsJob
            {
                /* RW */
                borderVertexPairs = borderVertexPairs.AsDeferredJobArray()
            }.Schedule(handle);

            handle = new FindRealPolygonJob
            {
                /* R  */
                borderVertexPairs = borderVertexPairs.AsDeferredJobArray(), // [ReadOnly] public NativeArray<MeshVertex> borderVertexPairs;
                /*  W */
                polygonVertices = polygonVertices.AsParallelWriter() // [WriteOnly] public NativeList<MeshVertex> polygonVertices;
            }.Schedule(borderVertexPairs, LOOP, handle);

            handle = new CalculateSolidificationJob
            {
                /* R  */
                polygonVertices = polygonVertices.AsDeferredJobArray(), // [ReadOnly] NativeArray<MeshVertex> polygonVertices;
                /*  W */
                solidificationTriangles = solidificationTriangles // [WriteOnly] NativeList<MeshTriangle> solidificationTriangles;
            }.Schedule(handle);

            handle = new BuildSolidTriangleListJob
            {
                triangles = triangles,
                solidificationTriangles = solidificationTriangles.AsDeferredJobArray(),
                solidTriangles = solidTriangles.AsParallelWriter()
            }.Schedule(tris.Length, LOOP, handle);

            handle = new BuildTriangleIndicesListJob
            {
                triangles = triangles,
                solidTriangles = solidTriangles.AsDeferredJobArray(),
                triangleIndices = triangleIndices,
                solidTriangleIndices = solidTriangleIndices
            }.Schedule(handle);

            handle = new CalculateCenterOfMassAndVolumeJob
            {
                triangles = triangles,
                solidTriangles = solidTriangles.AsDeferredJobArray(),
                vertices = vertices.AsDeferredJobArray(),
                centersOfMass = centersOfMass.AsParallelWriter(),
                solidCentersOfMass = solidCentersOfMass.AsParallelWriter(),
                solidVolumes = solidVolumes.AsParallelWriter(),
                volumes = volumes.AsParallelWriter()
            }.Schedule(solidTriangles, LOOP, handle);

            handle = new SumCenterOfMassAndVolumeJob
            {
                centersOfMass = centersOfMass.AsDeferredJobArray(),
                solidCentersOfMass = solidCentersOfMass.AsDeferredJobArray(),
                solidVolumes = solidVolumes.AsDeferredJobArray(),
                volumes = volumes.AsDeferredJobArray(),
                centerOfMass = centerOfMass_singleitem,
                solidCenterOfMass = solidCenterOfMass_singleitem,
                solidVolume = solidVolume_singleitem,
                volume = volume_singleitem
            }.Schedule(handle);

            handle = new CalculateSurfaceDataListJob
            {
                vertices = vertices.AsDeferredJobArray(),
                triangles = solidTriangles.AsDeferredJobArray(),
                triangleSurfaceAreas = solidTriangleSurfaceAreas.AsParallelWriter(),
                triangleFaceNormals = solidTriangleFaceNormals.AsParallelWriter(),
                triangleFaceMidpoints = solidTriangleFaceMidpoints.AsParallelWriter(),
                surfaceArea = solidSurfaceArea_singleitem
            }.Schedule(handle);

            JobHandle.ScheduleBatchedJobs();

            isCreated = true;
            isSolidified = true;

            handle.Complete();

            allEdges.Dispose();
            vertexHash.Dispose();
            originalVertexNormals.Dispose();
            originalVertexPositions.Dispose();
            originalTriangleIndices.Dispose();
            edgeHash.Dispose();
            borderEdgeIndexHash.Dispose();
            volumes.Dispose();
            centersOfMass.Dispose();
            borderVertexPairs.Dispose();
            polygonVertices.Dispose();
            solidificationTriangles.Dispose();
            solidVolumes.Dispose();
            solidCentersOfMass.Dispose();
        }

        [BurstCompile]
        private struct InitializeVertexJobs : IJob
        {
            public const int GROUPING_SCALE = 10000;

            [ReadOnly] public NativeArray<Vector3> verts;
            [ReadOnly] public NativeArray<Vector3> normals;

            public NativeHashMap<JobFloat3Key, int> vertexHash;
            public NativeList<MeshVertex> vertices;
            public NativeArray<BoundsBurst> boundsData;

            [WriteOnly] public NativeList<float3> vertexPositions;
            [WriteOnly] public NativeList<double> vertexPoints;
            [WriteOnly] public NativeArray<MeshSubvertex> subvertices;
            [WriteOnly] public NativeArray<int> originalToNewVertexMapping;

            public void Execute()
            {
                var bounds = boundsData[0];

                for (var index = 0; index < verts.Length; index++)
                {
                    var position = (float3) verts[index];

                    if (index == 0)
                    {
                        bounds.center = position;
                    }
                    else
                    {
                        bounds.Encapsulate(position);
                    }

                    var key = new JobFloat3Key(position, GROUPING_SCALE);

                    var newSubvertex = new MeshSubvertex(normals[index], GROUPING_SCALE, index);
                    subvertices[index] = newSubvertex;

                    if (vertexHash.ContainsKey(key))
                    {
                        originalToNewVertexMapping[index] = vertexHash[key];
                    }
                    else
                    {
                        originalToNewVertexMapping[index] = vertices.Length;

                        vertexHash.Add(key, vertices.Length);

                        var newVertex = new MeshVertex(position, key, index, vertices.Length);

                        vertices.Add(newVertex);
                        vertexPositions.Add(position);
                        vertexPoints.Add(position.x);
                        vertexPoints.Add(position.y);
                        vertexPoints.Add(position.z);
                    }
                }

                boundsData[0] = bounds;
            }
        }

        [BurstCompile]
        private struct PopulateTriangleDataJob_PF : IJobParallelFor
        {
            [ReadOnly, NativeDisableParallelForRestriction]
            public NativeArray<int> tris;

            [ReadOnly, NativeDisableParallelForRestriction]
            public NativeArray<int> originalToNewVertexMapping;

            [WriteOnly, NativeDisableParallelForRestriction]
            public NativeArray<MeshTriangle> triangles;

            [WriteOnly] public NativeList<int2>.ParallelWriter allEdges;

            public void Execute(int index)
            {
                if ((index % 3) != 0)
                {
                    return;
                }

                var originalXIndex = tris[index];
                var originalYIndex = tris[index + 1];
                var originalZIndex = tris[index + 2];

                var newXIndex = originalToNewVertexMapping[originalXIndex];
                var newYIndex = originalToNewVertexMapping[originalYIndex];
                var newZIndex = originalToNewVertexMapping[originalZIndex];

                var triIndex = index / 3;

                var triangle = new MeshTriangle(triIndex, originalXIndex, originalYIndex, originalZIndex, newXIndex, newYIndex, newZIndex);

                triangles[triIndex] = triangle;

                allEdges.AddNoResize(new int2(newXIndex, newYIndex));
                allEdges.AddNoResize(new int2(newXIndex, newZIndex));
                allEdges.AddNoResize(new int2(newYIndex, newZIndex));
            }
        }

        [BurstCompile]
        private struct CountEdgesJob : IJob
        {
            [ReadOnly] public NativeArray<int2> allEdges;

            public NativeHashMap<MeshEdge, int> edgeCounts;

            [WriteOnly] public NativeList<MeshEdge> edges;

            public void Execute()
            {
                for (var index = 0; index < allEdges.Length; index++)
                {
                    var edgeIndices = allEdges[index];

                    var index1 = edgeIndices.x;
                    var index2 = edgeIndices.y;

                    var edge = new MeshEdge(index1, index2);

                    if (edgeCounts.ContainsKey(edge))
                    {
                        var val = edgeCounts[edge];
                        val += 1;
                        edgeCounts[edge] = val;
                    }
                    else
                    {
                        edgeCounts.Add(edge, 1);
                        edges.Add(edge);
                    }
                }
            }
        }

        [BurstCompile]
        private struct RecreateEdgesJob : IJob
        {
            [ReadOnly] public NativeHashMap<MeshEdge, int> edgeTriangleCounts;

            public NativeArray<MeshEdge> edges;
            public NativeArray<int> borderEdgeCount;

            public void Execute()
            {
                for (var index = 0; index < edges.Length; index++)
                {
                    var edge = edges[index];

                    var newEdge = new MeshEdge(edge.aIndex, edge.bIndex, edgeTriangleCounts[edge]);
                    edges[index] = newEdge;

                    if (newEdge.triangleCount == 1)
                    {
                        borderEdgeCount[0] += 1;
                    }
                }
            }
        }

        [BurstCompile]
        private struct GetBorderEdgeIndicesJob_PF : IJobParallelForDefer
        {
            [ReadOnly] public NativeArray<MeshEdge> edges;
            [ReadOnly] public NativeArray<MeshVertex> vertices;

            [WriteOnly] public NativeList<int>.ParallelWriter borderEdgeIndices;
            [WriteOnly] public Core.Collections.Native.NativeHashSet<int2>.ParallelWriter borderEdgeIndexHash;

            public void Execute(int index)
            {
                var edge = edges[index];

                if (edge.aIndex == edge.bIndex)
                {
                    return;
                }

                var vertA = vertices[edge.aIndex];
                var vertB = vertices[edge.bIndex];

                if (vertA.position.Equals(vertB.position))
                {
                    return;
                }

                var ab = new int2(edge.aIndex, edge.bIndex);
                var ba = new int2(edge.bIndex, edge.aIndex);

                if (borderEdgeIndexHash.TryAdd(ab) && borderEdgeIndexHash.TryAdd(ba))
                {
                    if (edge.triangleCount == 1)
                    {
                        borderEdgeIndices.AddNoResize(index);
                    }
                }
            }
        }

        [BurstCompile]
        private struct TrimBorderVertexPairsJob : IJob
        {
            [ReadOnly] public NativeArray<int> borderEdgeIndices;
            [WriteOnly] public NativeList<MeshVertex> borderVertexPairs;

            public void Execute()
            {
                var length = borderEdgeIndices.Length;

                borderVertexPairs.Length = length * 2;
            }
        }

        [BurstCompile]
        private struct TrimBorderEdgeNormalsJob : IJob
        {
            [ReadOnly] public NativeArray<int> borderEdgeIndices;
            [WriteOnly] public NativeList<float3> borderEdgeNormals;

            public void Execute()
            {
                var length = borderEdgeIndices.Length;

                borderEdgeNormals.Length = length;
            }
        }

        [BurstCompile]
        private struct BuildBorderPolygonPairListJob_PF : IJobParallelForDefer
        {
            [ReadOnly] public NativeArray<MeshVertex> vertices;
            [ReadOnly] public NativeArray<int> borderEdgeIndices;
            [ReadOnly] public NativeArray<MeshEdge> edges;

            [WriteOnly, NativeDisableParallelForRestriction]
            public NativeArray<MeshVertex> borderVertexPairs;

            public void Execute(int index)
            {
                var edgeIndex = borderEdgeIndices[index];
                var edge = edges[edgeIndex];

                var vertexA = vertices[edge.aIndex];
                var vertexB = vertices[edge.bIndex];

                var baseIndex = index * 2;

                borderVertexPairs[baseIndex] = vertexA;
                borderVertexPairs[baseIndex + 1] = vertexB;
            }
        }

        [BurstCompile]
        private struct ReorderBorderPolygonsJob : IJob
        {
            public NativeArray<MeshVertex> borderVertexPairs;

            public void Execute()
            {
                var faceStart = 0;
                var i = 0;

                while (i < borderVertexPairs.Length)
                {
                    for (var j = i + 2; j < borderVertexPairs.Length; j += 2)
                    {
                        if (borderVertexPairs[j] == borderVertexPairs[i + 1])
                        {
                            SwitchPairs(borderVertexPairs, i + 2, j);
                            break;
                        }
                    }

                    if ((i + 3) >= borderVertexPairs.Length)
                    {
                        break;
                    }

                    if (borderVertexPairs[i + 3] == borderVertexPairs[faceStart])
                    {
                        i += 4;
                        faceStart = i;
                    }
                    else
                    {
                        i += 2;
                    }
                }
            }

            private static void SwitchPairs(NativeArray<MeshVertex> pairs, int pos1, int pos2)
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
        }

        [BurstCompile]
        private struct FindRealPolygonJob : IJobParallelForDefer
        {
            private const float threshold = 1e-6f;

            [ReadOnly] public NativeArray<MeshVertex> borderVertexPairs;
            [WriteOnly] public NativeList<MeshVertex>.ParallelWriter polygonVertices;

            public void Execute(int index)
            {
                if ((index % 2) != 0)
                {
                    return;
                }

                //for (var i = 0; i < borderVertexPairs.Length; i += 2)
                {
                    var edge1 = borderVertexPairs[index + 1].position - borderVertexPairs[index].position;
                    float3 edge2;
                    if (index == (borderVertexPairs.Length - 2))
                    {
                        edge2 = borderVertexPairs[1].position - borderVertexPairs[0].position;
                    }
                    else
                    {
                        edge2 = borderVertexPairs[index + 3].position - borderVertexPairs[index + 2].position;
                    }

                    edge1 = math.normalize(edge1);
                    edge2 = math.normalize(edge2);

                    if (Angle(edge1, edge2) > threshold)
                    {
                        polygonVertices.AddNoResize(borderVertexPairs[index + 1]);
                    }
                }
            }

            private static float Angle(float3 from, float3 to)
            {
                var sqrMagnitude = (double) math.lengthsq(from) * math.lengthsq(to);

                var magnitude = (float) math.sqrt(sqrMagnitude);

                var check = magnitude < 1.00000000362749E-15;

                return check ? 0.0f : (float) math.acos((double) math.clamp(math.dot(from, to) / magnitude, -1f, 1f)) * 57.29578f;
            }
        }

        [BurstCompile]
        private struct CalculateSolidificationJob : IJob
        {
            [ReadOnly] public NativeArray<MeshVertex> polygonVertices;

            [WriteOnly] public NativeList<MeshTriangle> solidificationTriangles;

            public void Execute()
            {
                solidificationTriangles.Clear();

                if (polygonVertices.Length == 0)
                {
                    return;
                }

                int tri_forward = 0, tri_backward = polygonVertices.Length - 1, tri_new = 1;
                var increment = true;

                while ((tri_new != tri_forward) && (tri_new != tri_backward))
                {
                    var vert_backward = polygonVertices[tri_backward];
                    var vert_forward = polygonVertices[tri_forward];
                    var vert_new = polygonVertices[tri_new];

                    var newTriangle = new MeshTriangle(
                        -1,
                        vert_backward.originalIndex,
                        vert_forward.originalIndex,
                        vert_new.originalIndex,
                        vert_backward.newIndex,
                        vert_forward.newIndex,
                        vert_new.newIndex
                    );

                    solidificationTriangles.Add(newTriangle);

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
        }

        [BurstCompile]
        private struct BuildSolidTriangleListJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<MeshTriangle> triangles;
            [ReadOnly] public NativeArray<MeshTriangle> solidificationTriangles;

            [WriteOnly] public NativeList<MeshTriangle>.ParallelWriter solidTriangles;

            public void Execute(int index)
            {
                if (index < triangles.Length)
                {
                    solidTriangles.AddNoResize(triangles[index]);
                }
                else if (index < (triangles.Length + solidificationTriangles.Length))
                {
                    solidTriangles.AddNoResize(solidificationTriangles[index - triangles.Length]);
                }
            }
        }

        [BurstCompile]
        private struct BuildTriangleIndicesListJob : IJob
        {
            [ReadOnly] public NativeArray<MeshTriangle> triangles;
            [ReadOnly] public NativeArray<MeshTriangle> solidTriangles;

            [WriteOnly] public NativeList<int> triangleIndices;
            [WriteOnly] public NativeList<int> solidTriangleIndices;

            public void Execute()
            {
                var maxIndex = math.max(triangles.Length, solidTriangles.Length);

                for (var index = 0; index < maxIndex; index++)
                {
                    if (index < triangles.Length)
                    {
                        var triangle = triangles[index];

                        triangleIndices.AddNoResize(triangle.xIndex);
                        triangleIndices.AddNoResize(triangle.yIndex);
                        triangleIndices.AddNoResize(triangle.zIndex);
                    }

                    if (index < solidTriangles.Length)
                    {
                        var triangle = solidTriangles[index];

                        solidTriangleIndices.AddNoResize(triangle.xIndex);
                        solidTriangleIndices.AddNoResize(triangle.yIndex);
                        solidTriangleIndices.AddNoResize(triangle.zIndex);
                    }
                }
            }
        }

        [BurstCompile]
        private struct CalculateFaceNormal : IJob
        {
            [ReadOnly] public NativeArray<MeshSubvertex> subvertices;
            [WriteOnly] public NativeArray<float3> faceNormal;

            public void Execute()
            {
                var norm = double3.zero;

                for (var i = 0; i < subvertices.Length; i++)
                {
                    var subvertex = subvertices[i];

                    norm += subvertex.normal;
                }

                var safeNorm = math.normalizesafe(norm);

                faceNormal[0] = new float3((float) safeNorm.x, (float) safeNorm.y, (float) safeNorm.z);
            }
        }

        [BurstCompile]
        private struct CalculateSurfaceDataJob : IJob
        {
            [ReadOnly] public NativeArray<MeshVertex> vertices;
            [ReadOnly] public NativeArray<MeshTriangle> triangles;
            [WriteOnly] public NativeArray<float> triangleSurfaceAreas;
            [WriteOnly] public NativeArray<float3> triangleFaceNormals;     
            [WriteOnly] public NativeArray<float3> triangleFaceMidpoints;          
            [WriteOnly] public NativeArray<float> surfaceArea;

            public void Execute()
            {
                var areaSum = 0f;

                for (var i = 0; i < triangles.Length; i++)
                {
                    var triangle = triangles[i];

                    var x = vertices[triangle.xIndex];
                    var y = vertices[triangle.yIndex];
                    var z = vertices[triangle.zIndex];
                    
                    var area = GetTriangleArea(x.position, y.position, z.position);
                    var normal = GetTriangleNormal(x.position, y.position, z.position);

                    areaSum += area;

                    triangleSurfaceAreas[i] = area;
                    triangleFaceNormals[i] = normal;
                    triangleFaceMidpoints[i] = (x.position + y.position + z.position) / 3.0f;
                }

                surfaceArea[0] = areaSum;
            }

            private static float GetTriangleArea(float3 p1, float3 p2, float3 p3)
            {
                var length1 = math.distance(p1, p2);
                var length2 = math.distance(p3, p1);

                var area = (length1 * length2 * math.sin(math.radians(mathex.angle(p2 - p1, p3 - p1)))) / 2f;

                return area;
            }

            private static float3 GetTriangleNormal(float3 p1, float3 p2, float3 p3)
            {
                var side2 = p2 - p1;
                var side3 = p3 - p1;

                var normal = math.normalize(math.cross(side2, side3));

                return normal;
            }
        }
        
        [BurstCompile]
        private struct CalculateSurfaceDataListJob : IJob
        {
            [ReadOnly] public NativeArray<MeshVertex> vertices;
            [ReadOnly] public NativeArray<MeshTriangle> triangles;
            [WriteOnly] public NativeList<float>.ParallelWriter triangleSurfaceAreas;
            [WriteOnly] public NativeList<float3>.ParallelWriter triangleFaceNormals;     
            [WriteOnly] public NativeList<float3>.ParallelWriter triangleFaceMidpoints;          
            [WriteOnly] public NativeArray<float> surfaceArea;

            public void Execute()
            {
                var areaSum = 0f;

                for (var i = 0; i < triangles.Length; i++)
                {
                    var triangle = triangles[i];

                    var x = vertices[triangle.xIndex];
                    var y = vertices[triangle.yIndex];
                    var z = vertices[triangle.zIndex];
                    
                    var area = GetTriangleArea(x.position, y.position, z.position);
                    var normal = GetTriangleNormal(x.position, y.position, z.position);

                    areaSum += area;

                    triangleSurfaceAreas.AddNoResize(area);
                    triangleFaceNormals.AddNoResize(normal);
                    triangleFaceMidpoints.AddNoResize((x.position + y.position + z.position) / 3.0f);
                }

                surfaceArea[0] = areaSum;
            }

            private static float GetTriangleArea(float3 p1, float3 p2, float3 p3)
            {
                var length1 = math.distance(p1, p2);
                var length2 = math.distance(p3, p1);

                var area = (length1 * length2 * math.sin(math.radians(mathex.angle(p2 - p1, p3 - p1)))) / 2f;

                return area;
            }

            private static float3 GetTriangleNormal(float3 p1, float3 p2, float3 p3)
            {
                var side2 = p2 - p1;
                var side3 = p3 - p1;

                var normal = math.normalize(math.cross(side2, side3));

                return normal;
            }
        }

        [BurstCompile]
        private struct CalculateBorderNormalJob_PF : IJobParallelForDefer
        {
            [ReadOnly] public NativeArray<MeshVertex> vertices;
            [ReadOnly] public NativeArray<int> borderEdgeIndices;
            [ReadOnly] public NativeArray<MeshEdge> edges;

            [WriteOnly] public NativeArray<float3> borderEdgeNormals;

            public void Execute(int index)
            {
                var edgeIndex = borderEdgeIndices[index];
                var edge = edges[edgeIndex];

                var vertexA = vertices[edge.aIndex].position;
                var vertexB = vertices[edge.bIndex].position;

                var normal = math.normalizesafe(math.cross(vertexB - vertexA, -vertexA));

                if (math.dot(normal, float3c.up) < 0)
                {
                    normal *= float3c.neg_one;
                }

                borderEdgeNormals[index] = normal;
            }
        }

        [BurstCompile]
        private struct NormalizeBorderNormalJob : IJob
        {
            [ReadOnly] public NativeArray<float3> borderEdgeNormals;
            [ReadOnly] public NativeArray<float3> faceNormal;

            [WriteOnly] public NativeArray<float3> borderEdgeNormal;

            public void Execute()
            {
                var borderNormal = double3.zero;
                var face = faceNormal[0];

                for (var i = 0; i < borderEdgeNormals.Length; i++)
                {
                    var normal = borderEdgeNormals[i];

                    borderNormal += new double3(normal);
                }

                var norm = math.normalizesafe(borderNormal);

                if (math.dot(face, norm) < 0)
                {
                    norm *= float3c.neg_one;
                }

                var normF = new float3((float) norm.x, (float) norm.y, (float) norm.z);
                borderEdgeNormal[0] = normF;
            }
        }

        [BurstCompile]
        private struct CalculateCenterOfMassAndVolumeJob : IJobParallelForDefer
        {
            [ReadOnly] public NativeArray<MeshTriangle> triangles;
            [ReadOnly] public NativeArray<MeshTriangle> solidTriangles;
            [ReadOnly] public NativeArray<MeshVertex> vertices;

            [WriteOnly] public NativeList<float>.ParallelWriter volumes;
            [WriteOnly] public NativeList<float3>.ParallelWriter centersOfMass;
            [WriteOnly] public NativeList<float>.ParallelWriter solidVolumes;
            [WriteOnly] public NativeList<float3>.ParallelWriter solidCentersOfMass;

            public void Execute(int index)
            {
                if (index < triangles.Length)
                {
                    Process(triangles[index], vertices, volumes, centersOfMass);
                }

                if (index < solidTriangles.Length)
                {
                    Process(solidTriangles[index], vertices, solidVolumes, solidCentersOfMass);
                }
            }

            private static void Process(
                MeshTriangle triangle,
                NativeArray<MeshVertex> vertices,
                NativeList<float>.ParallelWriter volumes,
                NativeList<float3>.ParallelWriter centersOfMass)
            {
                var v0i = triangle.xIndex;
                var v1i = triangle.yIndex;
                var v2i = triangle.zIndex;

                var v0 = vertices[v0i];
                var v1 = vertices[v1i];
                var v2 = vertices[v2i];

                var v = SignedVolumeOfTriangle(v0.position, v1.position, v2.position);

                centersOfMass.AddNoResize((v * (v0.position + v1.position + v2.position)) / 4f);
                volumes.AddNoResize(v);
            }

            private static float SignedVolumeOfTriangle(float3 p1, float3 p2, float3 p3)
            {
                return math.dot(p1, math.cross(p2, p3)) / 6.0f;
            }
        }

        [BurstCompile]
        private struct SumCenterOfMassAndVolumeJob : IJob
        {
            [ReadOnly] public NativeArray<float> volumes;
            [ReadOnly] public NativeArray<float3> centersOfMass;
            [ReadOnly] public NativeArray<float> solidVolumes;
            [ReadOnly] public NativeArray<float3> solidCentersOfMass;

            [WriteOnly] public NativeArray<float> volume;
            [WriteOnly] public NativeArray<float3> centerOfMass;
            [WriteOnly] public NativeArray<float> solidVolume;
            [WriteOnly] public NativeArray<float3> solidCenterOfMass;

            public void Execute()
            {
                var v = 0f;
                var com = float3.zero;
                var sv = 0f;
                var scom = float3.zero;

                for (var index = 0; index < solidVolumes.Length; index++)
                {
                    if (index < volumes.Length)
                    {
                        v += volumes[index];
                        com += centersOfMass[index];
                    }

                    sv += solidVolumes[index];
                    scom += solidCentersOfMass[index];
                }

                com /= centersOfMass.Length;
                scom /= solidCentersOfMass.Length;

                volume[0] = v;
                centerOfMass[0] = com;
                solidVolume[0] = sv;
                solidCenterOfMass[0] = scom;
            }
        }

        public void SafeDispose()
        {
            Dispose();
        }
        public void Dispose()
        {
            isCreated = false;

            SafeNative.SafeDispose(
                ref vertices,
                ref vertexPoints,
                ref vertexPositions,
                ref subvertices,
                ref originalToNewVertexMapping,
                ref edges,
                ref triangles,
                ref triangleSurfaceAreas,
                ref triangleFaceNormals,
                ref triangleFaceMidpoints,
                ref triangleIndices,
                ref borderEdgeCount,
                ref borderEdgeNormals,
                ref borderEdgeIndices,
                ref averageFaceNormal_singleitem,
                ref borderEdgeNormal_singleitem,
                ref boundsData_singleitem,
                ref volume_singleitem,
                ref centerOfMass_singleitem,
                ref surfaceArea_singleitem,
                ref solidTriangles,
                ref solidTriangleSurfaceAreas,
                ref solidTriangleFaceNormals,
                ref solidTriangleFaceMidpoints,
                ref solidTriangleIndices,
                ref solidVolume_singleitem,
                ref solidCenterOfMass_singleitem,
                ref solidSurfaceArea_singleitem
            );
        }
    }
}
