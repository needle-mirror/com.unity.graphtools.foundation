using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class CollapsibleInOutNode : Node, IDroppable, ICustomSearcherHandler
    {
        public static readonly string collapsedUssClassName = ussClassName.WithUssModifier("collapsed");
        public static readonly string collapseButtonPartName = "collapse-button";
        public static readonly string titleIconContainerPartName = "title-icon-container";

        const float k_ByteToPercentFactor = 100 / 255.0f;
        public byte Progress
        {
            set
            {
                var titleComponent = PartList.GetPart(titleContainerPartName) as IconTitleProgressPart;
                if (titleComponent?.CoroutineProgressBar != null)
                {
                    titleComponent.CoroutineProgressBar.value = value * k_ByteToPercentFactor;
                }
            }
        }

        protected override void BuildPartList()
        {
            PartList.AppendPart(IconTitleProgressPart.Create(titleIconContainerPartName, Model, this, ussClassName));
            PartList.AppendPart(InOutPortContainerPart.Create(portContainerPartName, Model, this, ussClassName));
        }

        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            var collapseButton = this.Q(collapseButtonPartName);
            collapseButton?.RegisterCallback<ChangeEvent<bool>>(OnCollapseChangeEvent);
        }

        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            bool collapsed = (NodeModel as ICollapsible)?.Collapsed ?? false;
            EnableInClassList(collapsedUssClassName, collapsed);
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
