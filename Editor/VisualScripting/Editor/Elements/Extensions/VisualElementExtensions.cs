using System;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor.UIElements;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Editor.ConstantEditor
{
    [PublicAPI]
    public static class VisualElementExtensions
    {
        public static VisualElement CreateEditorForNodeModel(this VisualElement element, IConstantNodeModel model, Action<IChangeEvent> onValueChanged)
        {
            VisualElement editorElement;

            var ext = ModelUtility.ExtensionMethodCache<IConstantEditorBuilder>.GetExtensionMethod(model.GetType(), ConstantEditorBuilder.FilterMethods, ConstantEditorBuilder.KeySelector);
            var graphAsset = model.AssetModel;
            if (ext != null)
            {
                Action<IChangeEvent> myValueChanged = evt =>
                {
                    if (evt != null) // Enum editor sends null
                    {
                        var p = evt.GetType().GetProperty("newValue");
                        var newValue = p.GetValue(evt);
                        ((ConstantNodeModel)model).ObjectValue = newValue;
                        onValueChanged(evt);
                    }

                    // TODO ugly but required to actually get the graph to be saved. Note that for some reason, the
                    // first edit works because the graph is already dirty
                    EditorUtility.SetDirty(graphAsset as Object);
                };
                var constantBuilder = new ConstantEditorBuilder(myValueChanged);
                editorElement = (VisualElement)ext.Invoke(null, new object[] { constantBuilder, model });
            }
            else
            {
                Debug.Log($"Could not draw Editor GUI for node of type {model.GetType()}");
                editorElement = new Label("<Unknown>");
            }

            return editorElement;
        }

        public static void RemovePropertyFieldValueLabel(this PropertyField propertyField)
        {
            // PropertyFields show a label saying "Value" by default which we don't really like
            // We only keep it in the case where the PropertyField has a "Foldout"
            // this ensures we have at least some text on folded property fields
            if (!(propertyField.Children().FirstOrDefault() is Foldout))
                propertyField.Q<Label>(classes: new[] { "unity-base-field__label" })?.RemoveFromHierarchy();
        }
    }
}
