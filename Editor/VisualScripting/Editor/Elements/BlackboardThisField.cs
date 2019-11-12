using System;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Editor.Highlighting;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;

namespace UnityEditor.VisualScripting.Editor
{
    public class BlackboardThisField : BlackboardField, IHighlightable, IVisualScriptingField, IHasGraphElementModel
    {
        readonly VseGraphView m_GraphView;

        public IGraphElementModel GraphElementModel => userData as IGraphElementModel;
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
            m_GraphView = graphView;
            userData = nodeModel;

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
            m_GraphView.HighlightGraphElements();
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            m_GraphView.ClearGraphElementsHighlight(ShouldHighlightItemUsage);
        }
    }
}
