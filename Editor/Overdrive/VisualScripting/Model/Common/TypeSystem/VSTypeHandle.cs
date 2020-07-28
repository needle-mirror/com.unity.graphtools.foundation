using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public static class VSTypeHandle
    {
        public static TypeHandle ThisType { get; }  = TypeSerializer.GenerateCustomTypeHandle("__THISTYPE");
    }
}
