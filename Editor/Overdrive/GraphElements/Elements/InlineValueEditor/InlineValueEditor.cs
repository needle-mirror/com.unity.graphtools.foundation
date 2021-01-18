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
            Action<IChangeEvent, object> onValueChanged, Store store)
        {
            return CreateEditorForConstant(model.AssetModel, null, model.Value, onValueChanged, store, model.IsLocked);
        }

        public static VisualElement CreateEditorForConstant(IGraphAssetModel graphAsset, IPortModel portModel, IConstant constant,
            Action<IChangeEvent, object> onValueChanged, Store store, bool modelIsLocked)
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

            var ext = ExtensionMethodCache<IConstantEditorBuilder>.GetExtensionMethod(constant.GetType(), ConstantEditorBuilder.FilterMethods, ConstantEditorBuilder.KeySelector);

            if (ext != null)
            {
                var constantBuilder = new ConstantEditorBuilder(myValueChanged, store, modelIsLocked, portModel);
                return (VisualElement)ext.Invoke(null, new object[] { constantBuilder, constant });
            }

            Debug.Log($"Could not draw Editor GUI for node of type {constant.Type}");
            return new Label("<Unknown>");
        }
    }
}
