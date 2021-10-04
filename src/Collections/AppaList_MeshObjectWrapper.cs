#region

using System;
using Appalachia.Core.Collections;

#endregion

namespace Appalachia.MeshData.Collections
{
    [Serializable]
    public sealed class AppaList_MeshObjectWrapper : AppaList<MeshObjectWrapper>
    {
        public AppaList_MeshObjectWrapper()
        {
        }

        public AppaList_MeshObjectWrapper(int capacity, float capacityIncreaseMultiplier = 2, bool noTracking = false) : base(
            capacity,
            capacityIncreaseMultiplier,
            noTracking
        )
        {
        }

        public AppaList_MeshObjectWrapper(AppaList<MeshObjectWrapper> list) : base(list)
        {
        }

        public AppaList_MeshObjectWrapper(MeshObjectWrapper[] values) : base(values)
        {
        }
    }
}
