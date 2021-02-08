using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IConstantEditorBuilder
    {
        Action<IChangeEvent> OnValueChanged { get; }
        CommandDispatcher CommandDispatcher { get; }
        bool ConstantIsLocked { get; }
        IPortModel PortModel { get; }
    }
}
