using System;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Highlighting;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class BlackboardThisField : BlackboardField, IHighlightable, IVisualScriptingField, IHasGraphElementModel
    {
        public IGraphElementModel GraphElementModel => Model as IGraphElementModel;
        public IGraphElementModel ExpandableGraphElementModel => null;

        public void Expand() {}
        public bool CanInstantiateInGraph() => true;

        public bool Highlighted
        {
            get => highlighted;
            set => highlighted = value;
        }

        public BlackboardThisField(VseGraphView graphView, ThisNodeModel nodeModel, IGraphModel graphModel)
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

        public bool ShouldHighlightItemUsage(IGraphElementModel candidate)
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
