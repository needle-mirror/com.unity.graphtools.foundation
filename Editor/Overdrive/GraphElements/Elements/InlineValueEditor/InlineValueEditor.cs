using System;
using JetBrains.Annotations;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    [PublicAPI]
    public static class InlineValueEditor
    {
        public static VisualElement CreateEditorForNodeModel(IGTFConstantNodeModel model,
            Action<IChangeEvent, object> onValueChanged, IGTFEditorDataModel editorDataModel)
        {
            return CreateEditorForConstant(((IConstantNodeModel)model).AssetModel, model.Value, onValueChanged, editorDataModel, model.IsLocked);
        }

        public static VisualElement CreateEditorForConstant(IGTFGraphAssetModel graphAsset, IConstant constant,
            Action<IChangeEvent, object> onValueChanged, IGTFEditorDataModel editorDataModel, bool modelIsLocked)
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

                // TODO ugly but required to actually get the graph to be saved. Note that for some reason, the
                // first edit works because the graph is already dirty
                // EditorUtility.SetDirty(graphAsset as Object);
            };

            var ext = ExtensionMethodCache<IConstantEditorBuilder>.GetExtensionMethod(constant.Type, ConstantEditorBuilder.FilterMethods, ConstantEditorBuilder.KeySelector);

            if (ext != null)
            {
                var constantBuilder = new ConstantEditorBuilder(myValueChanged, editorDataModel, modelIsLocked);
                return (VisualElement)ext.Invoke(null, new[] { constantBuilder, constant.ObjectValue });
            }

            if (constant is IStringWrapperConstantModel stringWrapperConstant)
            {
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
