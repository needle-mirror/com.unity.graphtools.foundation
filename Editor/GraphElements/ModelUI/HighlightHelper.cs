using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Extensions methods to highlight elements.
    /// </summary>
    public static class HighlightHelper
    {
        /// <summary>
        /// Removes the highlight on all elements.
        /// </summary>
        /// <param name="graphView">The graph view.</param>
        public static void ClearGraphElementsHighlight(this GraphView graphView)
        {
            var elements = graphView.Highlightables;
            foreach (var element in elements)
            {
                element.Highlighted = false;
            }
        }

        /// <summary>
        /// Removes the highlight on elements that match the predicate.
        /// </summary>
        /// <param name="graphView">The graph view.</param>
        /// <param name="predicate">A predicate that returns true if the highlight should be removed.</param>
        public static void ClearGraphElementsHighlight(this GraphView graphView,
            Func<IGraphElementModel, bool> predicate)
        {
            var elements = graphView.Highlightables;

            foreach (var element in elements)
            {
                if (element == null)
                {
                    continue;
                }

                if (predicate(element.Model))
                {
                    element.Highlighted = false;
                }
            }
        }

        /// <summary>
        /// Highlights the graph elements that need to be highlighted.
        /// </summary>
        /// <param name="graphView">The graph view.</param>
        public static void HighlightGraphElements(this GraphView graphView)
        {
            graphView.ClearGraphElementsHighlight();

            if (graphView.GetSelection().Count == 0)
            {
                return;
            }

            IEnumerable<IHighlightable> highlightables = graphView.Highlightables.ToList();

            // For all the selected items, highlight the graphElements that share the same declaration model
            // Exception: If the graphElement is selected, do not highlight it
            foreach (var selectable in graphView.GetSelection())
            {
                foreach (var highlightable in highlightables
                         .Where(h => (!Equals(selectable, h.Model) || !ReferenceEquals(selectable, h.Model)) &&
                             h.ShouldHighlightItemUsage(selectable)))
                {
                    highlightable.Highlighted = true;
                }
            }
        }
    }
}
