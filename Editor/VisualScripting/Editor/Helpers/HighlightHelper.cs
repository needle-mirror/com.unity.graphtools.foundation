using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor.Highlighting
{
    public static class HighlightHelper
    {
        public static void ClearGraphElementsHighlight(this VseGraphView graphView)
        {
            IEnumerable<IHighlightable> elements = GetHighlightables(graphView, true);
            foreach (var element in elements)
            {
                element.Highlighted = false;
            }
        }

        public static void ClearGraphElementsHighlight(this VseGraphView graphView,
            Func<IGraphElementModel, bool> predicate)
        {
            IEnumerable<IHighlightable> elements = GetHighlightables(graphView, true);

            foreach (var element in elements)
            {
                var hasGraphElementModel = element as IHasGraphElementModel;
                if (hasGraphElementModel == null)
                {
                    continue;
                }

                if (predicate(hasGraphElementModel.GraphElementModel))
                {
                    element.Highlighted = false;
                }
            }
        }

        public static void HighlightGraphElements(this VseGraphView graphView)
        {
            graphView.ClearGraphElementsHighlight();

            if (graphView.selection.Count == 0)
            {
                return;
            }

            IEnumerable<IHighlightable> highlightables = GetHighlightables(graphView, true).ToList();

            // For all the selected items, highlight the graphElements that share the same declaration model
            // Exception: If the graphElement is selected, do not highlight it
            foreach (ISelectable selectable in graphView.selection)
            {
                if (!(selectable is IHasGraphElementModel hasGraphElementModel))
                {
                    continue;
                }

                foreach (IHighlightable highlightable in highlightables
                         .Where(h => (!Equals(selectable, h) || !ReferenceEquals(selectable, h)) &&
                             h.ShouldHighlightItemUsage(hasGraphElementModel.GraphElementModel)))
                {
                    highlightable.Highlighted = true;
                }
            }
        }

        static IEnumerable<IHighlightable> GetHighlightables(VseGraphView graphView,
            bool includeBlackboard = false)
        {
            IEnumerable<IHighlightable> elements = graphView.graphElements.ToList()
                .OfType<IHighlightable>()
                .Where(x => x is IHasGraphElementModel);
            Blackboard blackboard = graphView.UIController.Blackboard;

            return includeBlackboard ? elements.Concat(blackboard.GraphVariables) : elements;
        }
    }
}
