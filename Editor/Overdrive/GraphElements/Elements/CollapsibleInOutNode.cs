using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class CollapsibleInOutNode : Node
    {
        public static readonly string k_CollapsedUssClassName = k_UssClassName.WithUssModifier("collapsed");

        public static readonly string k_CollapseButtonPartName = "collapse-button";

        protected override void BuildPartList()
        {
            var editableTitlePart = EditableTitlePart.Create(k_TitleContainerPartName, Model, this, k_UssClassName);
            PartList.AppendPart(editableTitlePart);
            PartList.AppendPart(InOutPortContainerPart.Create(k_PortContainerPartName, Model, this, k_UssClassName));

            var collapseButtonPart = NodeCollapseButtonPart.Create(k_CollapseButtonPartName, Model, this, k_UssClassName);
            // PF FIXME: move collapse button in EditableTitlePart
            if (collapseButtonPart != null)
            {
                if (editableTitlePart != null)
                {
                    editableTitlePart.PartList.AppendPart(collapseButtonPart);
                }
                else
                {
                    PartList.AppendPart(collapseButtonPart);
                }
            }
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

        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt) {}

        protected void OnCollapseChangeEvent(ChangeEvent<bool> evt)
        {
            Store.Dispatch(new SetNodeCollapsedAction(NodeModel, evt.newValue));
        }
    }
}
