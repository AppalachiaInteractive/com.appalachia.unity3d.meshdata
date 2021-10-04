#region

using System;
using System.Collections.Generic;
using System.Linq;
using Appalachia.Core.Attributes;
using Appalachia.Core.Collections.Native;
using Appalachia.MeshData.Collections;
using Unity.Profiling;
using UnityEngine;

#endregion

namespace Appalachia.MeshData
{
    [AlwaysInitializeOnLoad]
    public static class MeshObjectManager
    {
        private const string _PRF_PFX = nameof(MeshObjectManager) + ".";

        private static readonly ProfilerMarker _PRF_GetByMesh = new ProfilerMarker(_PRF_PFX + nameof(GetByMesh));
        //public static int groupingScale = 10000;

        private static MeshObjectWrapperLookup _meshes;
        private static MeshObjectWrapperLookup _soldifiedMeshes;

        private static List<Action> _completionActions;

        private static Dictionary<int, Mesh> _previousLookups = new Dictionary<int, Mesh>();

        static MeshObjectManager()
        {

        }
        
        [ExecuteOnEnable]
        static void Initialize() 
        {
            _meshes = new MeshObjectWrapperLookup();
            _soldifiedMeshes = new MeshObjectWrapperLookup();
        }


        private static readonly ProfilerMarker _PRF_GetByMesh_CheckCollection = new ProfilerMarker(_PRF_PFX + nameof(GetByMesh) + ".CheckCollection");
        private static readonly ProfilerMarker _PRF_GetByMesh_CreateWrapper = new ProfilerMarker(_PRF_PFX + nameof(GetByMesh) + ".CreateWrapper");
        private static readonly ProfilerMarker _PRF_GetByMesh_Initialize = new ProfilerMarker(_PRF_PFX + nameof(GetByMesh) + ".Initialize");
        private static readonly ProfilerMarker _PRF_GetByMesh_UpdateCollection = new ProfilerMarker(_PRF_PFX + nameof(GetByMesh) + ".UpdateCollection");
        //public static MeshObject GetByMesh(Mesh mesh)
        public static MeshObjectWrapper GetByMesh(Mesh mesh, bool solidified)
        {
            using (_PRF_GetByMesh.Auto())
            {
                try
                {
                    int hashCode;

                    using (_PRF_GetByMesh_Initialize.Auto())
                    {
                        if (_meshes == null)
                        {
                            _meshes = new MeshObjectWrapperLookup();
                        }

                        if (_soldifiedMeshes == null)
                        {
                            _soldifiedMeshes = new MeshObjectWrapperLookup();
                        }

                        hashCode = mesh.GetHashCode();
                    }

                    MeshObjectWrapper wrapper;
                    MeshObjectWrapperLookup collection;

                    using (_PRF_GetByMesh_CheckCollection.Auto())
                    {
                        collection = solidified ? _soldifiedMeshes : _meshes;

                        if (collection.ContainsKey(hashCode))
                        {
                            wrapper = collection.Get(hashCode);

                            if (wrapper.data.isCreated && !wrapper.data.vertices.ShouldAllocate())
                            {
                                return wrapper;
                            }
                        }

                    }
                    
                    using (_PRF_GetByMesh_CreateWrapper.Auto())
                    {
                        var uniqueName = $"{mesh.name}_{mesh.vertexCount}v_{mesh.triangles.Length}t";
                        wrapper = MeshObjectWrapper.LoadOrCreateNew(uniqueName);

                        wrapper.data = new MeshObject(mesh, solidified);
                        wrapper.mesh = mesh;
                        wrapper.SetDirty();
                    }

                    using (_PRF_GetByMesh_UpdateCollection.Auto())
                    {
                        collection.AddOrUpdate(hashCode, wrapper);
                    }

                    return wrapper;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to get mesh object: {ex}");
                    return default;
                }
            }
        }

        private static readonly ProfilerMarker _PRF_GetCheapestMesh = new ProfilerMarker(_PRF_PFX + nameof(GetCheapestMesh));
        public static Mesh GetCheapestMesh(GameObject obj)
        {
            using (_PRF_GetCheapestMesh.Auto())
            {
                if (_previousLookups == null)
                {
                    _previousLookups = new Dictionary<int, Mesh>();
                }

                var objHashCode = obj.GetHashCode();

                if (_previousLookups.ContainsKey(objHashCode))
                {
                    var prev = _previousLookups[objHashCode];
                    return prev;
                }

                MeshFilter meshFilter = null;

                var minimumVertexCount = 24;

                var lodGroup = obj.GetComponentInChildren<LODGroup>();

                if (lodGroup == null)
                {
                    var filters = obj.GetComponentsInChildren<MeshFilter>();

                    if (filters.Length == 0)
                    {
                        throw new NotSupportedException($"Missing mesh for {obj.name}");
                    }

                    var sortedFilters = filters.OrderBy(mf => mf.sharedMesh.vertexCount).ToArray();

                    meshFilter = sortedFilters.FirstOrDefault(mf => mf.sharedMesh.vertexCount > minimumVertexCount && !mf.sharedMesh.name.EndsWith("_GIZMO"));

                    if (meshFilter == null)
                    {
                        meshFilter = sortedFilters.FirstOrDefault();
                    }
                }
                else
                {
                    var lods = lodGroup.GetLODs();

                    for (var i = lods.Length - 1; i >= 0; i--)
                    {
                        var lod = lods[i];

                        if (lod.renderers.Length > 0)
                        {
                            var renderer = lod.renderers[0];

                            meshFilter = renderer.GetComponent<MeshFilter>();

                            if (meshFilter.sharedMesh.vertexCount > minimumVertexCount)
                            {
                                break;
                            }
                        }
                    }

                    if (meshFilter == null)
                    {
                        throw new NotSupportedException($"Missing mesh for {obj.name}");
                    }
                }

                var resultingMesh = meshFilter.sharedMesh;

                _previousLookups.Add(objHashCode, resultingMesh);

                return resultingMesh;
            }
        }

        private static readonly ProfilerMarker _PRF_GetCheapestMeshWrapper = new ProfilerMarker(_PRF_PFX + nameof(GetCheapestMeshWrapper));
        public static MeshObjectWrapper GetCheapestMeshWrapper(GameObject obj, bool solidified)
        {
            using (_PRF_GetCheapestMeshWrapper.Auto())
            {
                var mesh = GetCheapestMesh(obj);

                return GetByMesh(mesh, solidified);
            }
        }

        public static void RegisterDisposalDependency(Action a)
        {
            if (_completionActions == null)
            {
                _completionActions = new List<Action>();
            }

            _completionActions.Add(a);
        }
        
        private static readonly ProfilerMarker _PRF_DisposeNativeCollections = new ProfilerMarker(_PRF_PFX + nameof(DisposeNativeCollections));
        [ExecuteOnDisable]
        private static void DisposeNativeCollections()
        {
            using (_PRF_DisposeNativeCollections.Auto())
            {
                //Debug.Log("Disposing native collections.");

                if (_completionActions != null)
                {
                    for (var i = 0; i < _completionActions.Count; i++)
                    {
                        _completionActions[i]?.Invoke();
                    }
                }

                for (var i = 0; i < _meshes.Count; i++)
                {
                    var mesh = _meshes.GetByIndex(i);
                    mesh.data.Dispose();
                }
                
                for (var i = 0; i < _soldifiedMeshes.Count; i++)
                {
                    var mesh = _soldifiedMeshes.GetByIndex(i);
                    mesh.data.Dispose();
                }
            }
        }
    }
}
