using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class IconTitleProgressPart : EditableTitlePart
    {
        public static new readonly string ussClassName = "ge-icon-title-progress";
        public static readonly string collapseButtonPartName = "collapse-button";

        public static IconTitleProgressPart Create(string name, IGraphElementModel model, IModelUI modelUI, string parentClassName)
        {
            if (model is INodeModel)
            {
                return new IconTitleProgressPart(name, model, modelUI, parentClassName);
            }

            return null;
        }

        VisualElement m_Root;

        public override VisualElement Root => m_Root;

        public ProgressBar CoroutineProgressBar;

        protected IconTitleProgressPart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName, multiline: false)
        {
            if (model.IsCollapsible())
            {
                var collapseButtonPart = NodeCollapseButtonPart.Create(collapseButtonPartName, model, ownerElement, ussClassName);
                PartList.AppendPart(collapseButtonPart);
            }
        }

        protected override void BuildPartUI(VisualElement container)
        {
            if (!(m_Model is INodeModel nodeModel))
                return;

            m_Root = new VisualElement { name = PartName };
            m_Root.AddToClassList(ussClassName);
            m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            TitleContainer = new VisualElement();
            TitleContainer.AddToClassList(ussClassName.WithUssElement("title-container"));
            TitleContainer.AddToClassList(m_ParentClassName.WithUssElement("title-container"));
            m_Root.Add(TitleContainer);

            var icon = new VisualElement();
            icon.AddToClassList(ussClassName.WithUssElement("icon"));
            icon.AddToClassList(m_ParentClassName.WithUssElement("icon"));
            if (!string.IsNullOrEmpty(nodeModel.IconTypeString))
            {
                icon.AddToClassList(ussClassName.WithUssElement("icon").WithUssModifier(nodeModel.IconTypeString));
                icon.AddToClassList(m_ParentClassName.WithUssElement("icon").WithUssModifier(nodeModel.IconTypeString));
            }
            TitleContainer.Add(icon);

            if (HasEditableLabel)
            {
                TitleLabel = new EditableLabel { name = titleLabelName };
                TitleLabel.RegisterCallback<ChangeEvent<string>>(OnRename);
            }
            else
            {
                TitleLabel = new Label { name = titleLabelName };
            }

            TitleLabel.AddToClassList(ussClassName.WithUssElement("title"));
            TitleLabel.AddToClassList(m_ParentClassName.WithUssElement("title"));
            TitleContainer.Add(TitleLabel);

            if (nodeModel.HasProgress)
            {
                CoroutineProgressBar = new ProgressBar();
                CoroutineProgressBar.AddToClassList(ussClassName.WithUssElement("progress-bar"));
                CoroutineProgressBar.AddToClassList(m_ParentClassName.WithUssElement("progress-bar"));
                TitleContainer.Add(CoroutineProgressBar);
            }

            container.Add(m_Root);
        }

        protected override void PostBuildPartUI()
        {
            base.PostBuildPartUI();
            m_Root.AddStylesheet("IconTitleProgressPart.uss");
        }

        protected override void UpdatePartFromModel()
        {
            base.UpdatePartFromModel();

            var nodeModel = m_Model as INodeModel;
            if (nodeModel == null)
                return;

            CoroutineProgressBar?.EnableInClassList("hidden", !nodeModel.HasProgress);

            if (nodeModel.HasUserColor)
            {
                m_Root.style.backgroundColor = nodeModel.Color;
            }
            else
            {
                m_Root.style.backgroundColor = StyleKeyword.Null;
            }
        }
    }
}
