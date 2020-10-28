using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [PublicAPI]
    public static class InlineValueEditor
    {
        public static VisualElement CreateEditorForNodeModel(IConstantNodeModel model,
            Action<IChangeEvent, object> onValueChanged, IEditorDataModel editorDataModel)
        {
            return CreateEditorForConstant(model.AssetModel, model.Value, onValueChanged, editorDataModel, model.IsLocked);
        }

        public static VisualElement CreateEditorForConstant(IGraphAssetModel graphAsset, IConstant constant,
            Action<IChangeEvent, object> onValueChanged, IEditorDataModel editorDataModel, bool modelIsLocked)
        {
            Action<IChangeEvent> myValueChanged = evt =>
            {
                if (evt != null) // Enum editor sends null
                {
                    var p = evt.GetType().GetProperty("newValue");
                    var newValue = p.GetValue(evt);
                    if (constant is IStringWrapperConstantModel stringWrapperConstantModel)
                        stringWrapperConstantModel.StringValue = (string)newValue;
                    else
                        onValueChanged(evt, newValue);
                }
            };

            var ext = ExtensionMethodCache<IConstantEditorBuilder>.GetExtensionMethod(constant.Type, ConstantEditorBuilder.FilterMethods, ConstantEditorBuilder.KeySelector);

            if (ext != null)
            {
                var constantBuilder = new ConstantEditorBuilder(myValueChanged, editorDataModel, modelIsLocked);
                return (VisualElement)ext.Invoke(null, new[] { constantBuilder, constant.ObjectValue });
            }

            if (constant is IStringWrapperConstantModel stringWrapperConstant)
            {
                // PF: It should have been found by the previous call to GetExtensionMethod. Find why it did not work.
                ext = ExtensionMethodCache<IConstantEditorBuilder>.GetExtensionMethod(
                    typeof(IStringWrapperConstantModel), ConstantEditorBuilder.FilterMethods,
                    ConstantEditorBuilder.KeySelector);

                if (ext != null)
                {
                    var constantBuilder = new ConstantEditorBuilder(myValueChanged, editorDataModel, modelIsLocked);
                    return (VisualElement)ext.Invoke(null, new object[] {constantBuilder, stringWrapperConstant});
                }
            }

            Debug.Log($"Could not draw Editor GUI for node of type {constant.Type}");
            return new Label("<Unknown>");
        }
    }
}
