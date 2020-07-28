using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public static class HighlightHelper
    {
        public static void ClearGraphElementsHighlight(this GraphView graphView)
        {
            IEnumerable<IHighlightable> elements = graphView.Highlightables;
            foreach (var element in elements)
            {
                element.Highlighted = false;
            }
        }

        public static void ClearGraphElementsHighlight(this GraphView graphView,
            Func<IGTFGraphElementModel, bool> predicate)
        {
            IEnumerable<IHighlightable> elements = graphView.Highlightables;

            foreach (var element in elements)
            {
                var hasGraphElementModel = element as IGraphElement;
                if (hasGraphElementModel == null)
                {
                    continue;
                }

                if (predicate(hasGraphElementModel.Model))
                {
                    element.Highlighted = false;
                }
            }
        }

        public static void HighlightGraphElements(this GraphView graphView)
        {
            graphView.ClearGraphElementsHighlight();

            if (graphView.Selection.Count == 0)
            {
                return;
            }

            IEnumerable<IHighlightable> highlightables = graphView.Highlightables.ToList();

            // For all the selected items, highlight the graphElements that share the same declaration model
            // Exception: If the graphElement is selected, do not highlight it
            foreach (ISelectableGraphElement selectable in graphView.Selection)
            {
                if (!(selectable is IGraphElement hasGraphElementModel))
                {
                    continue;
                }

                foreach (IHighlightable highlightable in highlightables
                         .Where(h => (!Equals(selectable, h) || !ReferenceEquals(selectable, h)) &&
                             h.ShouldHighlightItemUsage(hasGraphElementModel.Model)))
                {
                    highlightable.Highlighted = true;
                }
            }
        }
    }
}
