using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public interface IConstantEditorBuilder
    {
        Action<IChangeEvent> OnValueChanged { get; }
        IGTFEditorDataModel EditorDataModel { get; }
        bool ConstantIsLocked { get; }
    }
}
