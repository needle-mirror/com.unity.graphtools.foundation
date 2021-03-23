using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IConstant
    {
        object ObjectValue { get; set; }
        object DefaultValue { get; }
        Type Type { get; }
    }
}
