using System;
using System.Collections.Generic;
using UnityEditor.VisualScripting.GraphViewModel;

namespace UnityEditor.VisualScripting.Model
{
    public interface IConstantNodeModel : IHasMainOutputPort
    {
        object ObjectValue { get; }
        bool IsLocked { get; }
        Type Type { get; }
    }

    public interface IStringWrapperConstantModel : IConstantNodeModel
    {
        List<string> GetAllInputNames();
        string StringValue { get; }
        string Label { get; }
    }
}
