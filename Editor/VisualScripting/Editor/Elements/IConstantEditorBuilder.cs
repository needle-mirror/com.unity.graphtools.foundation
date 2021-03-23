using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor.ConstantEditor
{
    public interface IConstantEditorBuilder
    {
        Action<IChangeEvent> OnValueChanged { get; }
    }
}
