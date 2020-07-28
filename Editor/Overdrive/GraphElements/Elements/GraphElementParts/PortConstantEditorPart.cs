using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class PortConstantEditorPart : BaseGraphElementPart
    {
        public static readonly string k_ConstantEditorUssName = "constant-editor";

        public static PortConstantEditorPart Create(string name, IGTFGraphElementModel model,
            IGraphElement graphElement, string parentClassName, IGTFEditorDataModel editorDataModel)
        {
            if (model is IGTFPortModel)
            {
                return new PortConstantEditorPart(name, model, graphElement, parentClassName, editorDataModel);
            }

            return null;
        }

        readonly IGTFEditorDataModel m_EditorDataModel;

        VisualElement m_Editor;

        Type m_EditorDataType;

        VisualElement m_Root;

        public override VisualElement Root => m_Root;

        protected PortConstantEditorPart(string name, IGTFGraphElementModel model, IGraphElement ownerElement,
                                         string parentClassName, IGTFEditorDataModel editorDataModel)
            : base(name, model, ownerElement, parentClassName)
        {
            m_EditorDataModel = editorDataModel;
        }

        protected override void BuildPartUI(VisualElement container)
        {
            var portModel = m_Model as IGTFPortModel;
            if (portModel != null && portModel.Direction == Direction.Input && portModel.EmbeddedValue != null)
            {
                m_Root = new VisualElement { name = PartName };
                m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

                BuildConstantEditor();

                container.Add(m_Root);
            }
        }

        protected override void UpdatePartFromModel()
        {
            if (m_Model is IGTFPortModel portModel)
            {
                BuildConstantEditor();
                m_Editor?.SetEnabled(!portModel.DisableEmbeddedValueEditor);
            }
        }

        void BuildConstantEditor()
        {
            if (m_Model is IGTFPortModel portModel)
            {
                // Rebuild editor if port data type changed.
                if (m_Editor != null && portModel.EmbeddedValue.Type != m_EditorDataType)
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
            if (m_Model is IGTFPortModel portModel)
            {
                m_OwnerElement.Store.Dispatch(new UpdatePortConstantAction(portModel, newValue));
            }
        }
    }
}
