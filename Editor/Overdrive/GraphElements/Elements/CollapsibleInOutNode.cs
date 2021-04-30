using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;
using UnityEngine.UIElements;
// ReSharper disable InconsistentNaming

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// UI for a <see cref="IInputOutputPortsNodeModel"/>.
    /// </summary>
    public class CollapsibleInOutNode : Node, ICustomSearcherHandler
    {
        public static readonly string collapsedUssClassName = ussClassName.WithUssModifier("collapsed");
        public static readonly string collapseButtonPartName = "collapse-button";
        public static readonly string titleIconContainerPartName = "title-icon-container";

        /// <summary>
        /// The name of the top container for vertical ports.
        /// </summary>
        public static readonly string topPortContainerPartName = "top-vertical-port-container";

        /// <summary>
        /// The name of the bottom container for vertical ports.
        /// </summary>
        public static readonly string bottomPortContainerPartName = "bottom-vertical-port-container";

        const float k_ByteToPercentFactor = 100 / 255.0f;
        public byte Progress
        {
            set
            {
                var titleComponent = PartList.GetPart(titleIconContainerPartName) as IconTitleProgressPart;
                if (titleComponent?.CoroutineProgressBar != null)
                {
                    titleComponent.CoroutineProgressBar.value = value * k_ByteToPercentFactor;
                }
            }
        }

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            PartList.AppendPart(VerticalPortContainerPart.Create(topPortContainerPartName, Direction.Input, Model, this, ussClassName));

            PartList.AppendPart(IconTitleProgressPart.Create(titleIconContainerPartName, Model, this, ussClassName));
            PartList.AppendPart(InOutPortContainerPart.Create(portContainerPartName, Model, this, ussClassName));

            PartList.AppendPart(VerticalPortContainerPart.Create(bottomPortContainerPartName, Direction.Output, Model, this, ussClassName));
        }

        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            var collapseButton = this.SafeQ(collapseButtonPartName);
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
            CommandDispatcher.Dispatch(new SetNodeCollapsedCommand(new[] { NodeModel }, evt.newValue));
        }

        public Func<Node, CommandDispatcher, Vector2, SearcherFilter, bool> CustomSearcherHandler { get; set; }

        public bool HandleCustomSearcher(Vector2 mousePosition, SearcherFilter filter = null)
        {
            return CustomSearcherHandler == null || CustomSearcherHandler(this, CommandDispatcher, mousePosition, filter);
        }
    }
}
