#region

using System;
using Appalachia.Core.Collections;
using Appalachia.Core.Collections.Implementations.Lists;
using Sirenix.OdinInspector;
using UnityEngine;

#endregion

namespace Appalachia.MeshData.Collections
{
    [Serializable]
    [ListDrawerSettings(Expanded = true, DraggableItems = false, HideAddButton = true, HideRemoveButton = true, NumberOfItemsPerPage = 5)]
    public class MeshObjectWrapperLookup : AppaLookup<int, MeshObjectWrapper, AppaList_int, AppaList_MeshObjectWrapper>
    {
        protected override string GetDisplayTitle(int key, MeshObjectWrapper value)
        {
            return value.mesh.name;
        }

        protected override string GetDisplaySubtitle(int key, MeshObjectWrapper value)
        {
            return string.Empty;
        }

        protected override Color GetDisplayColor(int key, MeshObjectWrapper value)
        {
            return Color.white;
        }
    }
}
