using System;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class ConstantNodeEditorPart : BaseGraphElementPart
    {
        public static ConstantNodeEditorPart Create(string name, IGraphElementModel model,
            IGraphElement graphElement, string parentClassName, IEditorDataModel editorDataModel)
        {
            if (model is ConstantNodeModel)
            {
                return new ConstantNodeEditorPart(name, model, graphElement, parentClassName, editorDataModel);
            }

            return null;
        }

        protected ConstantNodeEditorPart(string name, IGraphElementModel model, IGraphElement ownerElement,
                                         string parentClassName, IEditorDataModel editorDataModel)
            : base(name, model, ownerElement, parentClassName)
        {
            m_EditorDataModel = editorDataModel;
        }

        public static readonly string k_ConstantEditorUssName = "constant-editor";
        public static readonly string k_LabelUssName = "constant-editor-label";

        readonly IEditorDataModel m_EditorDataModel;
        Label m_Label;

        VisualElement m_Root;
        public override VisualElement Root => m_Root;

        protected override void BuildPartUI(VisualElement container)
        {
            if (m_Model is ConstantNodeModel constantNodeModel)
            {
                m_Root = new VisualElement { name = PartName };
                m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

                m_Label = new Label { name = k_LabelUssName };
                m_Label.AddToClassList(m_ParentClassName.WithUssElement(k_LabelUssName));
                m_Root.Add(m_Label);

                var tokenEditor = InlineValueEditor.CreateEditorForNodeModel(constantNodeModel, OnValueChanged, m_EditorDataModel);
                if (tokenEditor != null)
                {
                    tokenEditor.AddToClassList(m_ParentClassName.WithUssElement(k_ConstantEditorUssName));
                    m_Root.Add(tokenEditor);
                }
                container.Add(m_Root);
            }
        }

        protected override void UpdatePartFromModel()
        {
            if (m_Model is ConstantNodeModel constantNodeModel)
            {
                if (constantNodeModel.Value is IStringWrapperConstantModel icm)
                {
                    if (!TokenEditorNeedsLabel)
                        m_Label.style.display = DisplayStyle.None;
                    else
                        m_Label.text = icm.Label;
                }
            }
        }

        static TypeHandle[] s_PropsToHideLabel =
        {
            TypeHandle.Int,
            TypeHandle.Float,
            TypeHandle.Vector2,
            TypeHandle.Vector3,
            TypeHandle.Vector4,
            TypeHandle.String
        };

        bool TokenEditorNeedsLabel
        {
            get
            {
                if (m_Model is ConstantNodeModel constantNodeModel)
                    return !s_PropsToHideLabel.Contains(constantNodeModel.Type.GenerateTypeHandle());
                return true;
            }
        }

        void OnValueChanged(IChangeEvent evt, object arg3)
        {
            if (m_Model is ConstantNodeModel constantNodeModel)
            {
                if (evt != null) // Enum editor sends null
                {
                    var p = evt.GetType().GetProperty("newValue");
                    var newValue = p.GetValue(evt);
                    m_OwnerElement.Store.Dispatch(new UpdateConstantNodeValueAction(constantNodeModel.Value, newValue, constantNodeModel));
                }
            }
        }
    }
}
