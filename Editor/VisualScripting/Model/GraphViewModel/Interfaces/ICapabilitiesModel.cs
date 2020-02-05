using System;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    [Flags]
    [Serializable]
    public enum CapabilityFlags
    {
        Selectable         = 1 << 0,
        Collapsible        = 1 << 1,
        Resizable          = 1 << 2,
        Movable            = 1 << 3,
        Deletable          = 1 << 4,
        Droppable          = 1 << 5,
        Ascendable         = 1 << 6,
        Renamable          = 1 << 7,
        Modifiable         = 1 << 8,
        DeletableWhenEmpty = 1 << 9,
#if UNITY_2020_1_OR_NEWER
        Copiable           = 1 << 10
#endif
    }

    public interface ICapabilitiesModel
    {
        CapabilityFlags Capabilities { get; }
    }
}
