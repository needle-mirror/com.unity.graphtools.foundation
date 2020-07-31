using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class ExecutionFlow
    {
        ExecutionFlow() {}
    }

    public static class VSTypeHandle
    {
        public static TypeHandle ExecutionFlow { get; } = TypeSerializer.GenerateCustomTypeHandle(typeof(ExecutionFlow), "__EXECUTIONFLOW");
        public static TypeHandle ThisType { get; }  = TypeSerializer.GenerateCustomTypeHandle("__THISTYPE");
    }
}
