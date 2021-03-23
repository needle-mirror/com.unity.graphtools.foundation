using System;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class PortConstantEditorPart : BaseModelUIPart
    {
        public static readonly string constantEditorUssName = "constant-editor";

        public static PortConstantEditorPart Create(string name, IGraphElementModel model,
            IModelUI modelUI, string parentClassName)
        {
            if (model is IPortModel)
            {
                return new PortConstantEditorPart(name, model, modelUI, parentClassName);
            }

            return null;
        }

        VisualElement m_Editor;

        Type m_EditorDataType;

        VisualElement m_Root;

        public override VisualElement Root => m_Root;

        protected PortConstantEditorPart(string name, IGraphElementModel model, IModelUI ownerElement,
                                         string parentClassName)
            : base(name, model, ownerElement, parentClassName)
        {
        }

        protected override void BuildPartUI(VisualElement container)
        {
            var portModel = m_Model as IPortModel;
            if (portModel != null && portModel.Direction == Direction.Input)
            {
                InitRoot(container);
                if (portModel.EmbeddedValue != null)
                    BuildConstantEditor();
            }
        }

        private void InitRoot(VisualElement container)
        {
            m_Root = new VisualElement { name = PartName };
            m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            container.Add(m_Root);
        }

        protected override void UpdatePartFromModel()
        {
            if (m_Model is IPortModel portModel)
            {
                BuildConstantEditor();
                m_Editor?.SetEnabled(!portModel.DisableEmbeddedValueEditor);
            }
        }

        void BuildConstantEditor()
        {
            if (m_Model is IPortModel portModel)
            {
                // Rebuild editor if port data type changed.
                if (m_Editor != null && portModel.EmbeddedValue?.Type != m_EditorDataType)
                {
                    m_Editor.RemoveFromHierarchy();
                    m_Editor = null;
                }

                if (m_Editor == null)
                {
                    if (portModel.Direction == Direction.Input && portModel.EmbeddedValue != null)
                    {
                        m_EditorDataType = portModel.EmbeddedValue.Type;
                        m_Editor = InlineValueEditor.CreateEditorForConstant(m_Model.AssetModel, portModel, portModel.EmbeddedValue, OnValueChanged, m_OwnerElement.CommandDispatcher, false);
                        if (m_Editor != null)
                        {
                            m_Editor.AddToClassList(m_ParentClassName.WithUssElement(constantEditorUssName));
                            m_Root.Add(m_Editor);
                        }
                    }
                }
            }
        }

        void OnValueChanged(IChangeEvent evt, object newValue)
        {
            if (m_Model is IPortModel portModel)
            {
                m_OwnerElement.CommandDispatcher.Dispatch(new UpdatePortConstantCommand(portModel, newValue));
            }
        }
    }
}
