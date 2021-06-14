using System;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Profiling;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// VisualElement that controls how an edge is displayed. Designed to be added as a children to an <see cref="Edge"/>
    /// </summary>
    public class EdgeControl : VisualElement
    {
        protected struct BezierSegment
        {
            // P0 is previous segment last point.
            public Vector2 p1;
            public Vector2 p2;
            public Vector2 p3;
        }

        protected static CustomStyleProperty<int> s_EdgeWidthProperty = new CustomStyleProperty<int>("--edge-width");
        protected static CustomStyleProperty<Color> s_EdgeColorProperty = new CustomStyleProperty<Color>("--edge-color");
        protected static readonly int k_DefaultLineWidth = 2;
        protected static readonly Color k_DefaultColor = new Color(146 / 255f, 146 / 255f, 146 / 255f);
        protected static readonly float k_ContainsPointDistance = 25f;

        protected Edge m_Edge;

        protected VisualElement m_ControlPointContainer;

        protected List<BezierSegment> m_BezierSegments = new List<BezierSegment>();

        protected List<int> m_LineSegmentIndex = new List<int>();

        protected Mesh m_Mesh;

        protected PortOrientation m_InputOrientation;

        protected PortOrientation m_OutputOrientation;

        protected Color m_InputColor = Color.grey;

        protected Color m_OutputColor = Color.grey;

        protected bool m_ColorOverridden;

        protected bool m_WidthOverridden;

        protected int m_LineWidth = 2;

        public GraphView GraphView => m_Edge?.GraphView;

        protected int DefaultLineWidth { get; set; } = k_DefaultLineWidth;

        protected Color DefaultColor { get; set; } = k_DefaultColor;

        protected Edge EdgeParent => m_Edge ?? (m_Edge = GetFirstAncestorOfType<Edge>());

        // The start of the edge in graph coordinates.
        protected Vector2 From => EdgeParent?.From ?? Vector2.zero;

        // The end of the edge in graph coordinates.
        protected Vector2 To => EdgeParent?.To ?? Vector2.zero;

        public PortOrientation InputOrientation
        {
            get => m_InputOrientation;
            set
            {
                if (m_InputOrientation != value)
                {
                    m_InputOrientation = value;
                    MarkDirtyRepaint();
                }
            }
        }

        public PortOrientation OutputOrientation
        {
            get => m_OutputOrientation;
            set => m_OutputOrientation = value;
        }

        public Color InputColor
        {
            get => m_InputColor;
            private set
            {
                if (m_InputColor != value)
                {
                    m_InputColor = value;
                    MarkDirtyRepaint();
                }
            }
        }

        public Color OutputColor
        {
            get => m_OutputColor;
            private set
            {
                if (m_OutputColor != value)
                {
                    m_OutputColor = value;
                    MarkDirtyRepaint();
                }
            }
        }

        public int LineWidth
        {
            get => m_LineWidth;
            set
            {
                m_WidthOverridden = true;

                if (m_LineWidth == value)
                    return;

                m_LineWidth = value;
                UpdateLayout(); // The layout depends on the edges width
                MarkDirtyRepaint();
            }
        }

        public Vector2 ControlPointOffset { get; set; }

        // The points that will be rendered. Expressed in coordinates local to the element.
        public List<Vector2> RenderPoints { get; } = new List<Vector2>();

        /// <summary>
        /// Initializes a new instance of the <see cref="EdgeControl"/> class.
        /// </summary>
        public EdgeControl()
        {
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);

            pickingMode = PickingMode.Ignore;

            generateVisualContent += OnGenerateVisualContent;

            m_ControlPointContainer = new VisualElement { name = "control-points-container" };
            m_ControlPointContainer.style.position = Position.Absolute;
            pickingMode = PickingMode.Position;

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        protected void OnAttachToPanel(AttachToPanelEvent e)
        {
            parent.Add(m_ControlPointContainer);
        }

        protected void OnDetachFromPanel(DetachFromPanelEvent e)
        {
            m_ControlPointContainer.RemoveFromHierarchy();
        }

        protected void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            if (e.customStyle.TryGetValue(s_EdgeWidthProperty, out var edgeWidthValue))
                DefaultLineWidth = edgeWidthValue;

            if (e.customStyle.TryGetValue(s_EdgeColorProperty, out var edgeColorValue))
                DefaultColor = edgeColorValue;

            if (!m_WidthOverridden)
            {
                m_LineWidth = DefaultLineWidth;
                UpdateLayout(); // The layout depends on the edges width
                MarkDirtyRepaint();
            }

            if (!m_ColorOverridden)
            {
                m_InputColor = DefaultColor;
                m_OutputColor = DefaultColor;
                MarkDirtyRepaint();
            }
        }

        public void SetColor(Color inputColor, Color outputColor)
        {
            m_ColorOverridden = true;
            InputColor = inputColor;
            OutputColor = outputColor;
        }

        public void ResetColor()
        {
            m_ColorOverridden = false;
            InputColor = DefaultColor;
            OutputColor = DefaultColor;
        }

        public void FindNearestCurveSegment(Vector2 localPoint, out float minSquareDistance, out int nearestControlPointIndex, out int nearestRenderPointIndex)
        {
            minSquareDistance = Single.MaxValue;
            nearestRenderPointIndex = Int32.MaxValue;
            for (var index = 0; index < RenderPoints.Count - 1; index++)
            {
                var a = RenderPoints[index];
                var b = RenderPoints[index + 1];
                var squareDistance = SquaredDistanceToSegment(localPoint, a, b);
                if (squareDistance < minSquareDistance)
                {
                    minSquareDistance = squareDistance;
                    nearestRenderPointIndex = index;
                }
            }

            nearestControlPointIndex = 0;
            while (nearestControlPointIndex < m_LineSegmentIndex.Count && nearestRenderPointIndex >= m_LineSegmentIndex[nearestControlPointIndex])
            {
                nearestControlPointIndex++;
            }

            nearestControlPointIndex--;
        }

        static float SquaredDistanceToSegment(Vector2 p, Vector2 s0, Vector2 s1)
        {
            var x = p.x;
            var y = p.y;
            var x1 = s0.x;
            var y1 = s0.y;
            var x2 = s1.x;
            var y2 = s1.y;

            var a = x - x1;
            var b = y - y1;
            var c = x2 - x1;
            var d = y2 - y1;

            var dot = a * c + b * d;
            var lenSq = c * c + d * d;
            float param = -1;
            if (lenSq > float.Epsilon) //in case of 0 length line
                param = dot / lenSq;

            float xx, yy;

            if (param < 0)
            {
                xx = x1;
                yy = y1;
            }
            else if (param > 1)
            {
                xx = x2;
                yy = y2;
            }
            else
            {
                xx = x1 + param * c;
                yy = y1 + param * d;
            }

            var dx = x - xx;
            var dy = y - yy;
            return dx * dx + dy * dy;
        }

        public void RebuildControlPointsUI()
        {
            if (!(EdgeParent?.EdgeModel is IEditableEdge edgeModel))
                return;

            while (m_ControlPointContainer.childCount > edgeModel.EdgeControlPoints.Count)
            {
                m_ControlPointContainer.RemoveAt(m_ControlPointContainer.childCount - 1);
            }

            while (m_ControlPointContainer.childCount < edgeModel.EdgeControlPoints.Count)
            {
                var cp = new EdgeControlPoint(this, edgeModel, m_ControlPointContainer.childCount);
                m_ControlPointContainer.Add(cp);
            }
        }

        protected void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            Profiler.BeginSample("DrawEdge");
            DrawEdge(mgc);
            Profiler.EndSample();
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            FindNearestCurveSegment(localPoint, out var minDistance, out _, out _);
            return minDistance < k_ContainsPointDistance;
        }

        public override bool Overlaps(Rect rect)
        {
            if (base.Overlaps(rect))
            {
                for (int a = 0; a < RenderPoints.Count - 1; a++)
                {
                    if (RectUtils.IntersectsSegment(rect, RenderPoints[a], RenderPoints[a + 1]))
                        return true;
                }
            }

            return false;
        }

        public void UpdateLayout()
        {
            if (parent != null)
                ComputeLayout();
        }

        protected virtual void UpdateRenderPoints()
        {
            RenderPoints.Clear();
            m_LineSegmentIndex.Clear();

            Vector2 p0 = parent.ChangeCoordinatesTo(this, From);
            Vector2 p3 = parent.ChangeCoordinatesTo(this, To);
            for (var index = 0; index < m_BezierSegments.Count; index++)
            {
                var bezierSegment = m_BezierSegments[index];
                m_LineSegmentIndex.Add(RenderPoints.Count);

                Vector2 p1 = parent.ChangeCoordinatesTo(this, bezierSegment.p1);
                Vector2 p2 = parent.ChangeCoordinatesTo(this, bezierSegment.p2);
                p3 = parent.ChangeCoordinatesTo(this, bezierSegment.p3);

                int deepness = 0;
                GenerateRenderPoints(p0, p1, p2, p3, deepness);

                p0 = p3;
            }

            RenderPoints.Add(p3);
            m_LineSegmentIndex.Add(RenderPoints.Count);
        }

        static bool StraightEnough(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            // This computes an upper bound on the distance between the Bezier curve
            // and a straight line going from p0 to p3.
            // See https://hcklbrrfnn.files.wordpress.com/2012/08/bez.pdf
            // Summary: - define a straight Bezier line L going from p0 and p3 in terms of p0, p1, p2 and p3
            //          - subtract both curves: B - L =  (1 − t)t ((1 − t) u + t v)
            //          - compute the magnitude of the difference: D = ||B - L||^2
            //          - compute an upper bound on the magnitude: 1/16 * (Max(ux^2, vx^2) + Max(uy^2, vy^2))
            var u = 3 * p1 - 2 * p0 - p3;
            var v = 3 * p2 - 2 * p3 - p0;
            u = Vector2.Max(u, v);

            // Return true if the curve does not deviate from a straight line by more than 1.
            return u.x * u.x + u.y * u.y < 0.0625f;
        }

        void GenerateRenderPoints(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, int deepness)
        {
            if (StraightEnough(p0, p1, p2, p3) || deepness > 6)
            {
                RenderPoints.Add(p0);
                return;
            }

            // DeCasteljau algorithm.

            var midpoint = (p1 + p2) * 0.5f;
            var left1 = (p0 + p1) * 0.5f;
            var right2 = (p2 + p3) * 0.5f;

            var left2 = (left1 + midpoint) * 0.5f;
            var right1 = (right2 + midpoint) * 0.5f;

            var split = (left2 + right1) * 0.5f;

            GenerateRenderPoints(p0, left1, left2, split, deepness + 1);
            GenerateRenderPoints(split, right1, right2, p3, deepness + 1);
        }

        protected void ComputeLayout()
        {
            ComputeCurveSegmentsFromControlPoints();

            // Compute VisualElement position and dimension.
            var edgeModel = EdgeParent?.EdgeModel;

            if (edgeModel == null)
            {
                style.top = 0;
                style.left = 0;
                style.width = 0;
                style.height = 0;
                return;
            }

            Rect rect = new Rect(From, Vector2.zero);
            foreach (var bezierSegment in m_BezierSegments)
            {
                var pt = bezierSegment.p1;
                rect.xMin = Math.Min(rect.xMin, pt.x);
                rect.yMin = Math.Min(rect.yMin, pt.y);
                rect.xMax = Math.Max(rect.xMax, pt.x);
                rect.yMax = Math.Max(rect.yMax, pt.y);

                pt = bezierSegment.p2;
                rect.xMin = Math.Min(rect.xMin, pt.x);
                rect.yMin = Math.Min(rect.yMin, pt.y);
                rect.xMax = Math.Max(rect.xMax, pt.x);
                rect.yMax = Math.Max(rect.yMax, pt.y);

                pt = bezierSegment.p3;
                rect.xMin = Math.Min(rect.xMin, pt.x);
                rect.yMin = Math.Min(rect.yMin, pt.y);
                rect.xMax = Math.Max(rect.xMax, pt.x);
                rect.yMax = Math.Max(rect.yMax, pt.y);
            }

            var grow = Mathf.CeilToInt(LineWidth / 2.0f);
            rect.xMin -= grow;
            rect.xMax += grow;
            rect.yMin -= grow;
            rect.yMax += grow;

            var p = rect.position;
            var dim = rect.size;
            style.left = p.x;
            style.top = p.y;
            style.width = dim.x;
            style.height = dim.y;
        }

        void ComputeCurveSegmentsFromControlPoints()
        {
            if (EdgeParent == null)
                return;

            var edgeModel = EdgeParent?.EdgeModel;

            if (GraphView == null)
                return;

            var fromOrientation = EdgeParent.Output?.Orientation ?? EdgeParent.Input?.Orientation ?? PortOrientation.Horizontal;
            var toOrientation = EdgeParent.Input?.Orientation ?? fromOrientation;

            m_BezierSegments.Clear();

            var previous = From;
            var previousTightness = 1f;
            var directionFrom = fromOrientation == PortOrientation.Horizontal ? Vector2.right : Vector2.up;
            Vector2 directionTo;
            float length;

            if (edgeModel is IEditableEdge editableEdgeModel)
            {
                for (var i = 0; i < editableEdgeModel.EdgeControlPoints.Count; i++)
                {
                    var tightness = editableEdgeModel.EdgeControlPoints.ElementAt(i).Tightness / 100;

                    var splitPoint = editableEdgeModel.EdgeControlPoints.ElementAt(i).Position;
                    splitPoint += ControlPointOffset;
                    var localSplitPoint = GraphView.ContentViewContainer.ChangeCoordinatesTo(parent, splitPoint);
                    length = ControlPointDistance(previous, localSplitPoint, fromOrientation);

                    Vector2 next;
                    if (i == editableEdgeModel.EdgeControlPoints.Count - 1)
                    {
                        next = To;
                    }
                    else
                    {
                        next = editableEdgeModel.EdgeControlPoints.ElementAt(i + 1).Position;
                        next += ControlPointOffset;
                        next = GraphView.ContentViewContainer.ChangeCoordinatesTo(parent, next);
                    }

                    directionTo = (previous - next).normalized;

                    var segment = new BezierSegment
                    {
                        p1 = previous + directionFrom * (length * previousTightness),
                        p2 = localSplitPoint + directionTo * (length * tightness),
                        p3 = localSplitPoint,
                    };
                    m_BezierSegments.Add(segment);

                    previous = localSplitPoint;
                    previousTightness = tightness;
                    directionFrom = -directionTo;
                }
            }

            length = ControlPointDistance(previous, To, fromOrientation);
            directionTo = toOrientation == PortOrientation.Horizontal ? Vector2.left : Vector2.down;

            m_BezierSegments.Add(new BezierSegment()
            {
                p1 = previous + directionFrom * (length * previousTightness),
                p2 = To + directionTo * length,
                p3 = To,
            });

            // Update VisualElement positions for control point
            for (var i = 0; i < m_BezierSegments.Count - 1; i++)
            {
                if (i >= m_ControlPointContainer.childCount)
                    break;

                (m_ControlPointContainer[i] as EdgeControlPoint)?.SetPositions(m_BezierSegments[i].p3);
            }
        }

        // Compute the distance of Bezier curve control points P1 and P2 from P0 and P3 respectively.
        static float ControlPointDistance(Vector2 from, Vector2 to, PortOrientation orientation)
        {
            float xd, yd;
            if (orientation == PortOrientation.Horizontal)
            {
                xd = to.x - @from.x;
                yd = Mathf.Abs(to.y - @from.y);
            }
            else
            {
                xd = to.y - @from.y;
                yd = Mathf.Abs(to.x - @from.x);
            }

            // Max length is half the x distance.
            // When x distance is small or negative, we use a value based on the y distance mapped to [100, 250]
            var yCorr = 100f + Mathf.Min(150f, yd * .8f);
            float maxLength = Mathf.Max(xd, yCorr) * .5f;

            // When distance is small, we want the control points P1 and P2 to be near P0 and P3.
            // When distance is large, we want the control points P1 and P2 to be at maxLength from P0 and P3.
            var d = Mathf.Max(Mathf.Abs(xd), yd) * 0.01f;
            d *= d;
            var factor = d / (1f + d);

            return factor * maxLength;
        }

        protected void DrawEdge(MeshGenerationContext mgc)
        {
            if (LineWidth <= 0)
                return;

            UpdateRenderPoints();
            if (RenderPoints.Count == 0)
                return; // Don't draw anything

            Color inColor = InputColor;
            Color outColor = OutputColor;

#if UNITY_EDITOR
            inColor *= GraphViewStaticBridge.EditorPlayModeTint;
            outColor *= GraphViewStaticBridge.EditorPlayModeTint;
#endif // UNITY_EDITOR

            uint cpt = (uint)RenderPoints.Count;
            uint wantedLength = (cpt) * 2;
            uint indexCount = (wantedLength - 2) * 3;

            var md = GraphViewStaticBridge.AllocateMeshWriteData(mgc, (int)wantedLength, (int)indexCount);
            if (md.vertexCount == 0)
                return;

            float polyLineLength = 0;
            for (int i = 1; i < cpt; ++i)
                polyLineLength += (RenderPoints[i - 1] - RenderPoints[i]).sqrMagnitude;

            float halfWidth = LineWidth * 0.5f;
            float currentLength = 0;

            Vector2 unitPreviousSegment = Vector2.zero;
            for (int i = 0; i < cpt; ++i)
            {
                Vector2 dir;
                Vector2 unitNextSegment = Vector2.zero;
                Vector2 nextSegment = Vector2.zero;

                if (i < cpt - 1)
                {
                    nextSegment = (RenderPoints[i + 1] - RenderPoints[i]);
                    unitNextSegment = nextSegment.normalized;
                }


                if (i > 0 && i < cpt - 1)
                {
                    dir = unitPreviousSegment + unitNextSegment;
                    dir.Normalize();
                }
                else if (i > 0)
                {
                    dir = unitPreviousSegment;
                }
                else
                {
                    dir = unitNextSegment;
                }

                Vector2 pos = RenderPoints[i];
                Vector2 uv = new Vector2(dir.y * halfWidth, -dir.x * halfWidth); // Normal scaled by half width
                Color32 tint = Color.LerpUnclamped(outColor, inColor, currentLength / polyLineLength);

                md.SetNextVertex(new Vector3(pos.x, pos.y, 1), uv, tint);
                md.SetNextVertex(new Vector3(pos.x, pos.y, -1), uv, tint);

                if (i < cpt - 2)
                {
                    currentLength += nextSegment.sqrMagnitude;
                }
                else
                {
                    currentLength = polyLineLength;
                }

                unitPreviousSegment = unitNextSegment;
            }

            // Fill triangle indices as it is a triangle strip
            for (uint i = 0; i < wantedLength - 2; ++i)
            {
                if ((i & 0x01) == 0)
                {
                    md.SetNextIndex((UInt16)i);
                    md.SetNextIndex((UInt16)(i + 2));
                    md.SetNextIndex((UInt16)(i + 1));
                }
                else
                {
                    md.SetNextIndex((UInt16)i);
                    md.SetNextIndex((UInt16)(i + 1));
                    md.SetNextIndex((UInt16)(i + 2));
                }
            }
        }

        protected void OnLeavePanel(DetachFromPanelEvent e)
        {
            if (m_Mesh != null)
            {
                UnityEngine.Object.DestroyImmediate(m_Mesh);
                m_Mesh = null;
            }
        }
    }
}
