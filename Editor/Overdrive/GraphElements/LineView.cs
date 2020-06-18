using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    class LineView : VisualElement
    {
        public LineView()
        {
            this.StretchToParentSize();
            generateVisualContent += OnGenerateVisualContent;
        }

        public List<Line> lines { get; } = new List<Line>();

        void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            var gView = GetFirstAncestorOfType<GraphView>();
            if (gView == null)
            {
                return;
            }
            var container = gView.contentViewContainer;
            foreach (var line in lines)
            {
                var start = container.ChangeCoordinatesTo(gView, line.Start);
                var end = container.ChangeCoordinatesTo(gView, line.End);
                var x = Math.Min(start.x, end.x);
                var y = Math.Min(start.y, end.y);
                var width = Math.Max(1, Math.Abs(start.x - end.x));
                var height = Math.Max(1, Math.Abs(start.y - end.y));
                var rect = new Rect(x, y, width, height);

                GraphViewStaticBridge.SolidRectangle(mgc, rect, GraphViewSettings.UserSettings.SnappingLineColor, ContextType.Editor);
            }
        }
    }
}
