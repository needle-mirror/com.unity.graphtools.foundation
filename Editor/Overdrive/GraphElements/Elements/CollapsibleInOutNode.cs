using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class CollapsibleInOutNode : Node, IDroppable, ICustomSearcherHandler
    {
        public static readonly string k_CollapsedUssClassName = k_UssClassName.WithUssModifier("collapsed");
        public static readonly string k_CollapseButtonPartName = "collapse-button";
        public static readonly string k_TitleIconContainerPartName = "title-icon-container";

        const float k_ByteToPercentFactor = 100 / 255.0f;
        public byte Progress
        {
            set
            {
                var titleComponent = PartList.GetPart(k_TitleContainerPartName) as IconTitleProgressPart;
                if (titleComponent?.CoroutineProgressBar != null)
                {
                    titleComponent.CoroutineProgressBar.value = value * k_ByteToPercentFactor;
                }
            }
        }

        protected override void BuildPartList()
        {
            PartList.AppendPart(IconTitleProgressPart.Create(k_TitleIconContainerPartName, Model, this, k_UssClassName));
            PartList.AppendPart(InOutPortContainerPart.Create(k_PortContainerPartName, Model, this, k_UssClassName));
        }

        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            var collapseButton = this.Q(k_CollapseButtonPartName);
            collapseButton?.RegisterCallback<ChangeEvent<bool>>(OnCollapseChangeEvent);
        }

        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            bool collapsed = (NodeModel as ICollapsible)?.Collapsed ?? false;
            EnableInClassList(k_CollapsedUssClassName, collapsed);
        }

        protected void OnCollapseChangeEvent(ChangeEvent<bool> evt)
        {
            Store.Dispatch(new SetNodeCollapsedAction(new[] { NodeModel}, evt.newValue));
        }

        public Func<Node, Store, Vector2, SearcherFilter, bool> CustomSearcherHandler { get; set; }

        public bool HandleCustomSearcher(Vector2 mousePosition, SearcherFilter filter = null)
        {
            return CustomSearcherHandler == null || CustomSearcherHandler(this, Store, mousePosition, filter);
        }
    }
}
