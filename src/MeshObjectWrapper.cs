#region

using System;
using Appalachia.Base.Scriptables;
using UnityEngine;

#endregion

namespace Appalachia.MeshData
{
    [Serializable]
    public class MeshObjectWrapper : SelfSavingScriptableObject<MeshObjectWrapper>
    {
        [NonSerialized] public MeshObject data;

        [SerializeField] private Mesh _mesh;

        public Mesh mesh
        {
            get => _mesh;
            set
            {
                _mesh = value;
                SetDirty();
            }
        }

        public MeshObjectWrapper()
        {
            MeshObjectManager.RegisterDisposalDependency(() => data.SafeDispose());
        }

        public bool isCreated => data.isCreated;

        public MeshObject CreateAndGetData(bool solidify)
        {
            data = new MeshObject(mesh, solidify);

            SetDirty();
            
            return data;
        }
    }
}
