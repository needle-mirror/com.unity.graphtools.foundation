using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class AutoSpacingHelper : AutoPlacementHelper
    {
        Orientation m_Orientation;

        public AutoSpacingHelper(GraphView graphView)
        {
            m_GraphView = graphView;
        }

        public void SendSpacingAction(Orientation orientation)
        {
            m_Orientation = orientation;

            // Get spacing delta for each element
            Dictionary<IGraphElementModel, Vector2> results = GetElementDeltaResults();

            // Dispatch action
            SendPlacementAction(results.Keys.ToList(), results.Values.ToList());
        }

        protected override float GetStartingPosition(List<Tuple<Rect, List<IGraphElementModel>>> boundingRects)
        {
            return m_Orientation == Orientation.Horizontal ? boundingRects.First().Item1.xMin : boundingRects.First().Item1.yMin;
        }

        protected override void UpdateReferencePosition(ref float referencePosition, Rect currentElementRect)
        {
            referencePosition += (m_Orientation == Orientation.Horizontal ? currentElementRect.width : currentElementRect.height) + GraphViewSettings.UserSettings.SpacingMarginValue;
        }

        protected override Vector2 GetDelta(Rect elementPosition, float referencePosition)
        {
            float offset = referencePosition - (m_Orientation == Orientation.Horizontal ? elementPosition.x : elementPosition.y);

            return m_Orientation == Orientation.Horizontal ? new Vector2(offset, 0f) : new Vector2(0f, offset);
        }

        protected override List<Tuple<Rect, List<IGraphElementModel>>> GetBoundingRectsList(List<Tuple<Rect, List<IGraphElementModel>>> boundingRects)
        {
            return boundingRects.OrderBy(rect => m_Orientation == Orientation.Horizontal ? rect.Item1.x : rect.Item1.y).ToList();
        }
    }
}
