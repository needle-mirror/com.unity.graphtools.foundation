using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine.UIElements;
using IRenamable = UnityEditor.GraphToolsFoundation.Overdrive.Model.IRenamable;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class EditableTitlePart : BaseGraphElementPart
    {
        public static EditableTitlePart Create(string name, IGTFGraphElementModel model, IGraphElement graphElement, string parentClassName, bool multiline = false)
        {
            if (model is IHasTitle)
            {
                return new EditableTitlePart(name, model, graphElement, parentClassName, multiline);
            }

            return null;
        }

        public static readonly string k_UssClassName = "ge-editable-title-part";
        public static readonly string k_TitleLabelName = "title";

        bool m_Multiline;

        protected EditableTitlePart(string name, IGTFGraphElementModel model, IGraphElement ownerElement, string parentClassName, bool multiline)
            : base(name, model, ownerElement, parentClassName)
        {
            m_Multiline = multiline;
        }

        protected VisualElement TitleContainer { get; set; }
        protected VisualElement TitleLabel { get; set; }

        public override VisualElement Root => TitleContainer;

        protected override void BuildPartUI(VisualElement container)
        {
            if (m_Model is IHasTitle)
            {
                bool isRenamable = m_Model is IRenamable renamable && renamable.IsRenamable;
                TitleContainer = new VisualElement { name = PartName };
                TitleContainer.AddToClassList(k_UssClassName);
                TitleContainer.AddToClassList(m_ParentClassName.WithUssElement(PartName));

                if (isRenamable)
                {
                    TitleLabel = new EditableLabel { name = k_TitleLabelName, multiline = m_Multiline };
                    TitleLabel.RegisterCallback<ChangeEvent<string>>(OnRename);
                }
                else
                {
                    TitleLabel = new Label { name = k_TitleLabelName };
                }

                TitleLabel.AddToClassList(k_UssClassName.WithUssElement(k_TitleLabelName));
                TitleLabel.AddToClassList(m_ParentClassName.WithUssElement(k_TitleLabelName));
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
            m_OwnerElement.Store.Dispatch(new RenameElementAction(m_Model as IRenamable, e.newValue));
        }
    }
}
