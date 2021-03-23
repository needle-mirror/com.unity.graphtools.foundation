using System;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class EditableTitlePart : BaseModelUIPart
    {
        public static readonly string ussClassName = "ge-editable-title-part";
        public static readonly string titleLabelName = "title";

        public static EditableTitlePart Create(string name, IGraphElementModel model, IModelUI modelUI, string parentClassName, bool multiline = false)
        {
            if (model is IHasTitle)
            {
                return new EditableTitlePart(name, model, modelUI, parentClassName, multiline);
            }

            return null;
        }

        bool m_Multiline;

        protected VisualElement TitleContainer { get; set; }

        public VisualElement TitleLabel { get; protected set; }

        public override VisualElement Root => TitleContainer;

        protected EditableTitlePart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName, bool multiline)
            : base(name, model, ownerElement, parentClassName)
        {
            m_Multiline = multiline;
        }

        protected virtual bool HasEditableLabel => m_Model.IsRenamable();

        protected override void BuildPartUI(VisualElement container)
        {
            if (m_Model is IHasTitle)
            {
                TitleContainer = new VisualElement { name = PartName };
                TitleContainer.AddToClassList(ussClassName);
                TitleContainer.AddToClassList(m_ParentClassName.WithUssElement(PartName));

                if (HasEditableLabel)
                {
                    TitleLabel = new EditableLabel { name = titleLabelName, multiline = m_Multiline };
                    TitleLabel.RegisterCallback<ChangeEvent<string>>(OnRename);
                }
                else
                {
                    TitleLabel = new Label { name = titleLabelName };
                }

                TitleLabel.AddToClassList(ussClassName.WithUssElement(titleLabelName));
                TitleLabel.AddToClassList(m_ParentClassName.WithUssElement(titleLabelName));
                TitleContainer.Add(TitleLabel);

                container.Add(TitleContainer);
            }
        }

        protected override void UpdatePartFromModel()
        {
            if (TitleLabel != null)
            {
                var value = (m_Model as IHasTitle)?.DisplayTitle ?? String.Empty;
                if (TitleLabel is EditableLabel editableLabel)
                    editableLabel.SetValueWithoutNotify(value);
                else if (TitleLabel is Label label)
                    label.text = value;
            }
        }

        protected override void PostBuildPartUI()
        {
            base.PostBuildPartUI();
            TitleContainer.AddStylesheet("EditableTitlePart.uss");
        }

        protected void OnRename(ChangeEvent<string> e)
        {
            m_OwnerElement.CommandDispatcher.Dispatch(new RenameElementCommand(m_Model as IRenamable, e.newValue));
        }
    }
}
