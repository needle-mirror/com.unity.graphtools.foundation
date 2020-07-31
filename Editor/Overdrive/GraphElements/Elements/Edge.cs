using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class Edge : GraphElement, IMovableGraphElement
    {
        public new static readonly string k_UssClassName = "ge-edge";
        public static readonly string k_EditModeModifierUssClassName = k_UssClassName.WithUssModifier("edit-mode");
        public static readonly string k_GhostModifierUssClassName = k_UssClassName.WithUssModifier("ghost");

        public static readonly string k_EdgeControlPartName = "edge-control";
        public static readonly string k_EdgeBubblePartName = "edge-bubble";

        public IGTFEdgeModel EdgeModel => Model as IGTFEdgeModel;

        protected EdgeManipulator m_EdgeManipulator;
        protected ContextualMenuManipulator m_ContextualMenuManipulator;

        public bool IsGhostEdge => EdgeModel is IGhostEdge;

        public Vector2 From
        {
            get
            {
                var p = Vector2.zero;

                var port = EdgeModel.FromPort;
                if (port == null)
                {
                    if (EdgeModel is IGhostEdge ghostEdgeModel)
                    {
                        p = ghostEdgeModel.EndPoint;
                    }
                }
                else
                {
                    var ui = port.GetUI<Port>(GraphView);
                    if (ui == null)
                        return Vector2.zero;

                    p = ui.GetGlobalCenter();
                }

                return this.WorldToLocal(p);
            }
        }

        public Vector2 To
        {
            get
            {
                var p = Vector2.zero;

                var port = EdgeModel.ToPort;
                if (port == null)
                {
                    if (EdgeModel is GhostEdgeModel ghostEdgeModel)
                    {
                        p = ghostEdgeModel.EndPoint;
                    }
                }
                else
                {
                    var ui = port.GetUI<Port>(GraphView);
                    if (ui == null)
                        return Vector2.zero;

                    p = ui.GetGlobalCenter();
                }

                return this.WorldToLocal(p);
            }
        }

        EdgeControl m_EdgeControl;
        public EdgeControl EdgeControl
        {
            get
            {
                if (m_EdgeControl == null)
                {
                    var edgeControlPart = PartList.GetPart(k_EdgeControlPartName);
                    m_EdgeControl = edgeControlPart?.Root as EdgeControl;
                }

                return m_EdgeControl;
            }
        }

        public IGTFPortModel Output => EdgeModel.FromPort;

        public IGTFPortModel Input => EdgeModel.ToPort;

        public override bool ShowInMiniMap => false;

        public Edge()
        {
            layer = -1;

            m_EdgeManipulator = new EdgeManipulator();
            this.AddManipulator(m_EdgeManipulator);

            m_ContextualMenuManipulator = new ContextualMenuManipulator(BuildContextualMenu);
            this.AddManipulator(m_ContextualMenuManipulator);
        }

        protected override void BuildPartList()
        {
            PartList.AppendPart(EdgeControlPart.Create(k_EdgeControlPartName, Model, this, k_UssClassName));
            PartList.AppendPart(EdgeBubblePart.Create(k_EdgeBubblePartName, Model, this, k_UssClassName));
        }

        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            EdgeControl?.RegisterCallback<GeometryChangedEvent>(OnEdgeGeometryChanged);

            AddToClassList(k_UssClassName);
            EnableInClassList(k_GhostModifierUssClassName, IsGhostEdge);
            this.AddStylesheet("Edge.uss");
        }

        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();
            EnableInClassList(k_EditModeModifierUssClassName, EdgeModel.EditMode);
        }

        protected virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt) {}

        public override bool Overlaps(Rect rectangle)
        {
            return EdgeControl.Overlaps(this.ChangeCoordinatesTo(EdgeControl, rectangle));
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            return EdgeControl.ContainsPoint(this.ChangeCoordinatesTo(EdgeControl, localPoint));
        }

        public override void OnSelected()
        {
            base.OnSelected();

            var edgeControlPart = PartList.GetPart(k_EdgeControlPartName);
            edgeControlPart?.UpdateFromModel();

            ((IHasPorts)EdgeModel.FromPort.NodeModel).RevealReorderableEdgesOrder(true, EdgeModel);
            // TODO JOCE: This is required until we have a dirtying mechanism (see ShowConnectedExecutionEdgesOrder in NodeModel.cs)
            EdgeModel.FromPort.NodeModel.GetUI<Node>(GraphView)?.UpdateOutgoingExecutionEdges();
        }

        public override void OnUnselected()
        {
            base.OnUnselected();

            var edgeControlPart = PartList.GetPart(k_EdgeControlPartName);
            edgeControlPart?.UpdateFromModel();

            if (EdgeModel.FromPort != null)
            {
                ((IHasPorts)EdgeModel.FromPort.NodeModel).RevealReorderableEdgesOrder(false);

                // TODO JOCE: This is required until we have a dirtying mechanism (see ShowConnectedExecutionEdgesOrder in NodeModel.cs)
                EdgeModel.FromPort.NodeModel.GetUI<Node>(GraphView)?.UpdateOutgoingExecutionEdges();
            }
        }

        void OnEdgeGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateFromModel();
        }

        public bool IsMovable => true;
    }
}
