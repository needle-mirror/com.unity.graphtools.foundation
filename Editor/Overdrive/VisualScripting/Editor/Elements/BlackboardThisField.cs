using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class BlackboardThisField : BlackboardField, IHighlightable, IVisualScriptingField
    {
        public IGTFGraphElementModel ExpandableGraphElementModel => null;

        public void Expand() {}
        public bool CanInstantiateInGraph() => true;

        public bool Highlighted
        {
            get => highlighted;
            set => highlighted = value;
        }

        public BlackboardThisField(GraphView graphView, ThisNodeModel nodeModel, IGTFGraphModel graphModel)
        {
            SetupBuildAndUpdate(nodeModel, null, graphView);

            text = "This";

            var pill = this.MandatoryQ<Pill>("pill");
            pill.tooltip = text;

            typeText = graphModel?.FriendlyScriptName;

            viewDataKey = "blackboardThisFieldKey";
        }

        public override bool IsRenamable()
        {
            return false;
        }

        public bool ShouldHighlightItemUsage(IGTFGraphElementModel candidate)
        {
            return candidate is ThisNodeModel;
        }

        public override void OnSelected()
        {
            base.OnSelected();
            (GraphView as VseGraphView).HighlightGraphElements();
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            (GraphView as VseGraphView).ClearGraphElementsHighlight(ShouldHighlightItemUsage);
        }
    }
}
