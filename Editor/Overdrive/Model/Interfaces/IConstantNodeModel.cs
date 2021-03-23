using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IConstantNodeModel : ISingleOutputPortNodeModel, IHasMainOutputPort
    {
        // Type safe value set.
        void SetValue<T>(T value);
        object ObjectValue { get; set; }
        Type Type { get; }
        bool IsLocked { get; set; }
        IConstant Value { get; }
        void PredefineSetup();
    }
}
