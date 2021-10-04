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
        [SerializeField] private Mesh _mesh;
        [NonSerialized] public MeshObject data;

        public MeshObjectWrapper()
        {
            MeshObjectManager.RegisterDisposalDependency(() => data.SafeDispose());
        }

        public Mesh mesh
        {
            get => _mesh;
            set
            {
                _mesh = value;
                SetDirty();
            }
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
