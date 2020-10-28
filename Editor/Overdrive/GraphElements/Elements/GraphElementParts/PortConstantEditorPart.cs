using System;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class PortConstantEditorPart : BaseGraphElementPart
    {
        public static readonly string k_ConstantEditorUssName = "constant-editor";

        public static PortConstantEditorPart Create(string name, IGraphElementModel model,
            IGraphElement graphElement, string parentClassName, IEditorDataModel editorDataModel)
        {
            if (model is IPortModel)
            {
                return new PortConstantEditorPart(name, model, graphElement, parentClassName, editorDataModel);
            }

            return null;
        }

        readonly IEditorDataModel m_EditorDataModel;

        VisualElement m_Editor;

        Type m_EditorDataType;

        VisualElement m_Root;

        public override VisualElement Root => m_Root;

        protected PortConstantEditorPart(string name, IGraphElementModel model, IGraphElement ownerElement,
                                         string parentClassName, IEditorDataModel editorDataModel)
            : base(name, model, ownerElement, parentClassName)
        {
            m_EditorDataModel = editorDataModel;
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
            m_Root = new VisualElement {name = PartName};
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
                        m_Editor = InlineValueEditor.CreateEditorForConstant(m_Model.GraphModel.AssetModel, portModel.EmbeddedValue, OnValueChanged, m_EditorDataModel, false);
                        if (m_Editor != null)
                        {
                            m_Editor.AddToClassList(m_ParentClassName.WithUssElement(k_ConstantEditorUssName));
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
                m_OwnerElement.Store.Dispatch(new UpdatePortConstantAction(portModel, newValue));
            }
        }
    }
}
