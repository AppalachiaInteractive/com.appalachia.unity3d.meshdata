#region

using System;
using Appalachia.Core.MeshData;

#endregion

namespace Appalachia.Core.Collections.Implementations.Lists
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
