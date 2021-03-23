using System;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    public interface IObjectReference
    {
        Object ReferencedObject { get; }
    }
}
