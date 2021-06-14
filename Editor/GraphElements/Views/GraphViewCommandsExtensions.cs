using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Extension methods on <see cref="GraphView"/> to help dispatching commands.
    /// </summary>
    public static class GraphViewCommandsExtensions
    {
        /// <summary>
        /// Dispatches a command to delete the selected elements from the graph model.
        /// </summary>
        /// <param name="self">The graph view representing the graph model.</param>
        /// <param name="operationName">The name of the operation to display in the Undo menu.</param>
        public static void DispatchDeleteSelectionCommand(this GraphView self, string operationName = "Delete")
        {
            self.CommandDispatcher.Dispatch(new DeleteElementsCommand(self.GetSelection()) { UndoString = operationName });
        }

        /// <summary>
        /// Dispatches a <see cref="ReframeGraphViewCommand"/> to show all graph elements.
        /// </summary>
        /// <param name="self">The graph view.</param>
        public static void DispatchFrameAllCommand(this GraphView self)
        {
            var rectToFit = self.CalculateRectToFitAll(self.ContentViewContainer);
            self.CalculateFrameTransform(rectToFit, self.layout, GraphView.k_FrameBorder, out var frameTranslation, out var frameScaling);
            self.CommandDispatcher.Dispatch(new ReframeGraphViewCommand(frameTranslation, frameScaling));
        }

        /// <summary>
        /// Dispatches a <see cref="ReframeGraphViewCommand"/> to show all selected graph elements.
        /// </summary>
        /// <param name="self">The graph view.</param>
        public static void DispatchFrameSelectionCommand(this GraphView self)
        {
            var graphElements = new List<GraphElement>();
            foreach (var model in self.GetSelection())
            {
                var ui = model.GetUI<GraphElement>(self);
                if (ui != null)
                    graphElements.Add(ui);
            }

            if (graphElements.Count > 0)
                DispatchFrameAndSelectElementsCommand(self, false, graphElements);
        }

        /// <summary>
        /// Dispatch a command to select and frame the previous graph element that matches the predicate.
        /// Elements are ordered by their order of creation.
        /// </summary>
        /// <param name="self">The GraphView.</param>
        /// <param name="predicate">A function to filter the graph elements.</param>
        public static void DispatchFramePrevCommand(this GraphView self, Func<GraphElement, bool> predicate)
        {
            var list = self.GraphElements.ToList().
                Where(e => e.Model.IsSelectable() && !(e.Model is IEdgeModel) && predicate(e)).
                OrderByDescending(e => e.GetControlId()).
                ToList();

            DispatchFrameNextAndSelectElementCommand(self, list);
        }

        /// <summary>
        /// Dispatch a command to select and frame the next graph element that matches the predicate.
        /// Elements are ordered by their order of creation.
        /// </summary>
        /// <param name="self">The GraphView.</param>
        /// <param name="predicate">A function to filter the graph elements.</param>
        public static void DispatchFrameNextCommand(this GraphView self, Func<GraphElement, bool> predicate)
        {
            var list = self.GraphElements.ToList().
                Where(e => e.Model.IsSelectable() && !(e.Model is IEdgeModel) && predicate(e)).
                OrderBy(e => e.GetControlId()).
                ToList();

            DispatchFrameNextAndSelectElementCommand(self, list);
        }

        static void DispatchFrameNextAndSelectElementCommand(GraphView graphView, List<GraphElement> graphElementList)
        {
            if (graphElementList.Count == 0)
                return;

            var selectedModel = graphView.GetSelection().FirstOrDefault();
            int idx = graphElementList.FindIndex(e => ReferenceEquals(e.Model, selectedModel));
            var graphElement = idx >= 0 && idx < graphElementList.Count - 1 ? graphElementList[idx + 1] : graphElementList[0];

            DispatchFrameAndSelectElementsCommand(graphView, true, graphElement);
        }

        static Rect CalculateRectToFitElements(GraphView graphView, IReadOnlyList<GraphElement> graphElements)
        {
            Rect rectToFit = graphView.ContentViewContainer.layout;

            if (graphElements == null || graphElements.Count == 0)
            {
                return rectToFit;
            }

            VisualElement graphElement = graphElements[0];

            if (graphElement != null)
            {
                // Edges don't have a size. Only their internal EdgeControl have a size.
                if (graphElement is Edge edge)
                    graphElement = edge.EdgeControl;

                rectToFit = graphElement.ChangeCoordinatesTo(graphView.ContentViewContainer, graphElement.GetRect());
            }

            rectToFit = graphElements.Aggregate(rectToFit, (current, currentGraphElement) =>
            {
                VisualElement currentElement = currentGraphElement;

                if (currentGraphElement is Edge edge)
                    currentElement = edge.EdgeControl;

                return RectUtils.Encompass(current, currentElement.ChangeCoordinatesTo(graphView.ContentViewContainer, currentElement.GetRect()));
            });

            return rectToFit;
        }

        /// <summary>
        /// Dispatch a command to frame and optionally select a list of graph elements.
        /// </summary>
        /// <param name="self">The GraphView.</param>
        /// <param name="graphElements">The list of elements to frame and optionally select.</param>
        /// <param name="select">True if the elements should be selected. False if the selection should not change.</param>
        public static void DispatchFrameAndSelectElementsCommand(this GraphView self, bool select, params GraphElement[] graphElements)
        {
            DispatchFrameAndSelectElementsCommand(self, select, (IReadOnlyList<GraphElement>)graphElements);
        }

        /// <summary>
        /// Dispatch a command to frame and optionally select a list of graph elements.
        /// </summary>
        /// <param name="self">The GraphView.</param>
        /// <param name="graphElements">The list of elements to frame and optionally select.</param>
        /// <param name="select">True if the elements should be selected. False if the selection should not change.</param>
        public static void DispatchFrameAndSelectElementsCommand(this GraphView self, bool select, IReadOnlyList<GraphElement> graphElements)
        {
            if (!graphElements.Any())
                return;

            var rectToFit = CalculateRectToFitElements(self, graphElements);
            self.CalculateFrameTransform(rectToFit, self.layout, GraphView.k_FrameBorder, out var frameTranslation, out var frameScaling);

            self.CommandDispatcher.Dispatch(new ReframeGraphViewCommand(frameTranslation, frameScaling,
                select ? graphElements.Select(e => e.Model).ToList() : null));
        }
    }
}
