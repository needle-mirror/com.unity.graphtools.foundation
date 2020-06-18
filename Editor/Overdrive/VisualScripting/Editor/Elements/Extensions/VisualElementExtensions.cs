using System;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [PublicAPI]
    public static class VisualElementExtensions
    {
        public static VisualElement CreateEditorForNodeModel(IConstantNodeModel model,
            Action<IChangeEvent> onValueChanged, IEditorDataModel editorDataModel)
        {
            VisualElement editorElement;

            var ext = ExtensionMethodCache<IConstantEditorBuilder>.GetExtensionMethod(model.GetType(), ConstantEditorBuilder.FilterMethods, ConstantEditorBuilder.KeySelector);
            var graphAsset = model.AssetModel;
            if (ext != null)
            {
                Action<IChangeEvent> myValueChanged = evt =>
                {
                    if (evt != null) // Enum editor sends null
                    {
                        var p = evt.GetType().GetProperty("newValue");
                        var newValue = p.GetValue(evt);
                        if (model is IStringWrapperConstantModel stringWrapperConstantModel)
                            stringWrapperConstantModel.StringValue = (string)newValue;
                        else
                            ((ConstantNodeModel)model).ObjectValue = newValue;
                        onValueChanged(evt);
                    }

                    // TODO ugly but required to actually get the graph to be saved. Note that for some reason, the
                    // first edit works because the graph is already dirty
                    EditorUtility.SetDirty(graphAsset as Object);
                };
                var constantBuilder = new ConstantEditorBuilder(myValueChanged, editorDataModel);
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
