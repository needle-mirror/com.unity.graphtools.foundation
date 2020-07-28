using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Model
{
    public interface IConstant
    {
        object ObjectValue { get; set; }
        object DefaultValue { get; }
        Type Type { get; }
    }
}
