using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IStringWrapperConstantModel : IConstant
    {
        string StringValue { get; set; }
        string Label { get; }
    }
}
