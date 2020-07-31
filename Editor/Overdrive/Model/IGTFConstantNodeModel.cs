using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Model
{
    public interface IGTFConstantNodeModel : IGTFNodeModel, IHasSingleOutputPort
    {
        // Type safe value set.
        void SetValue<T>(T value);
        object ObjectValue { get; set; }
        Type Type { get; }
        bool IsLocked { get; }
        IConstant Value { get; }
    }

    public interface IGTFStringWrapperConstantModel : IConstant
    {
        string StringValue { get; set; }
    }
}
