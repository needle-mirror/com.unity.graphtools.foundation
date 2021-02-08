using System;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class ConstantNodeEditorPart : BaseModelUIPart
    {
        public static ConstantNodeEditorPart Create(string name, IGraphElementModel model,
            IModelUI modelUI, string parentClassName)
        {
            if (model is IConstantNodeModel)
            {
                return new ConstantNodeEditorPart(name, model, modelUI, parentClassName);
            }

            return null;
        }

        protected ConstantNodeEditorPart(string name, IGraphElementModel model, IModelUI ownerElement,
                                         string parentClassName)
            : base(name, model, ownerElement, parentClassName)
        {
        }

        public static readonly string constantEditorElementUssClassName = "constant-editor";
        public static readonly string labelUssName = "constant-editor-label";

        Label m_Label;

        VisualElement m_Root;
        public override VisualElement Root => m_Root;

        protected override void BuildPartUI(VisualElement container)
        {
            if (m_Model is IConstantNodeModel constantNodeModel)
            {
                m_Root = new VisualElement { name = PartName };
                m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

                m_Label = new Label { name = labelUssName };
                m_Label.AddToClassList(m_ParentClassName.WithUssElement(labelUssName));
                m_Root.Add(m_Label);

                var tokenEditor = InlineValueEditor.CreateEditorForNodeModel(constantNodeModel, OnValueChanged, m_OwnerElement.CommandDispatcher);
                if (tokenEditor != null)
                {
                    tokenEditor.AddToClassList(m_ParentClassName.WithUssElement(constantEditorElementUssClassName));
                    m_Root.Add(tokenEditor);
                }
                container.Add(m_Root);
            }
        }

        protected override void UpdatePartFromModel()
        {
            if (m_Model is IConstantNodeModel constantNodeModel)
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
                if (m_Model is IConstantNodeModel constantNodeModel)
                    return !s_PropsToHideLabel.Contains(constantNodeModel.Type.GenerateTypeHandle());
                return true;
            }
        }

        void OnValueChanged(IChangeEvent evt, object arg3)
        {
            if (m_Model is IConstantNodeModel constantNodeModel)
            {
                if (evt != null) // Enum editor sends null
                {
                    var p = evt.GetType().GetProperty("newValue");
                    var newValue = p.GetValue(evt);
                    m_OwnerElement.CommandDispatcher.Dispatch(new UpdateConstantNodeValueCommand(constantNodeModel.Value, newValue, constantNodeModel));
                }
            }
        }
    }
}
