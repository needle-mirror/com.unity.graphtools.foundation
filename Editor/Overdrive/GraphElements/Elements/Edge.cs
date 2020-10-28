using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.InternalModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class Edge : GraphElement
    {
        public new static readonly string k_UssClassName = "ge-edge";
        public static readonly string k_EditModeModifierUssClassName = k_UssClassName.WithUssModifier("edit-mode");
        public static readonly string k_GhostModifierUssClassName = k_UssClassName.WithUssModifier("ghost");

        public static readonly string k_EdgeControlPartName = "edge-control";
        public static readonly string k_EdgeBubblePartName = "edge-bubble";

        protected EdgeManipulator m_EdgeManipulator;

        EdgeControl m_EdgeControl;

        public IEdgeModel EdgeModel => Model as IEdgeModel;

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

        public IPortModel Output => EdgeModel.FromPort;

        public IPortModel Input => EdgeModel.ToPort;

        public override bool ShowInMiniMap => false;

        public Edge()
        {
            Layer = -1;

            m_EdgeManipulator = new EdgeManipulator();
            this.AddManipulator(m_EdgeManipulator);
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

            if (EdgeModel is IEditableEdge editableEdge)
                EnableInClassList(k_EditModeModifierUssClassName, editableEdge.EditMode);
        }

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

            EdgeModel.FromPort.NodeModel.RevealReorderableEdgesOrder(true, EdgeModel);

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
                EdgeModel.FromPort.NodeModel.RevealReorderableEdgesOrder(false);

                // TODO JOCE: This is required until we have a dirtying mechanism (see ShowConnectedExecutionEdgesOrder in NodeModel.cs)
                EdgeModel.FromPort.NodeModel.GetUI<Node>(GraphView)?.UpdateOutgoingExecutionEdges();
            }
        }

        void OnEdgeGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateFromModel();
        }

        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            if (!(evt.currentTarget is Edge edge))
                return;

            var editableEdge = edge.EdgeModel as IEditableEdge;

            if (editableEdge?.EditMode ?? false)
            {
                if (evt.menu.MenuItems().Count > 0)
                    evt.menu.AppendSeparator();

                var p = edge.EdgeControl.WorldToLocal(evt.triggerEvent.originalMousePosition);
                edge.EdgeControl.FindNearestCurveSegment(p, out _, out var controlPointIndex, out _);
                p = edge.WorldToLocal(evt.triggerEvent.originalMousePosition);

                if (!(evt.target is EdgeControlPoint))
                {
                    evt.menu.AppendAction("Add Control Point", menuAction =>
                    {
                        Store.Dispatch(new AddControlPointOnEdgeAction(editableEdge, controlPointIndex, p));
                    });
                }

                evt.menu.AppendAction("Stop Editing Edge", menuAction =>
                {
                    Store.Dispatch(new SetEdgeEditModeAction(editableEdge, false));
                });
            }
            else
            {
                bool initialSeparatorAdded = false;
                int initialMenuItemCount = evt.menu.MenuItems().Count;

                if (editableEdge != null)
                {
                    if (initialMenuItemCount > 0)
                    {
                        initialSeparatorAdded = true;
                        evt.menu.AppendSeparator();
                    }

                    evt.menu.AppendAction("Edit Edge", menuAction =>
                    {
                        Store.Dispatch(new SetEdgeEditModeAction(editableEdge, true));
                    });
                }

                if ((edge.EdgeModel.FromPort as IReorderableEdgesPort)?.HasReorderableEdges ?? false)
                {
                    if (!initialSeparatorAdded && initialMenuItemCount > 0)
                        evt.menu.AppendSeparator();

                    var siblingEdges = edge.EdgeModel.FromPort.GetConnectedEdges().ToList();
                    var siblingEdgesCount = siblingEdges.Count;

                    var index = siblingEdges.IndexOf(edge.EdgeModel);
                    evt.menu.AppendAction("Reorder Edge/Move First",
                        a => ReorderEdges(ReorderEdgeAction.ReorderType.MoveFirst),
                        siblingEdgesCount > 1 && index > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                    evt.menu.AppendAction("Reorder Edge/Move Up",
                        a => ReorderEdges(ReorderEdgeAction.ReorderType.MoveUp),
                        siblingEdgesCount > 1 && index > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                    evt.menu.AppendAction("Reorder Edge/Move Down",
                        a => ReorderEdges(ReorderEdgeAction.ReorderType.MoveDown),
                        siblingEdgesCount > 1 && index < siblingEdgesCount - 1 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                    evt.menu.AppendAction("Reorder Edge/Move Last",
                        a => ReorderEdges(ReorderEdgeAction.ReorderType.MoveLast),
                        siblingEdgesCount > 1 && index < siblingEdgesCount - 1 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                    void ReorderEdges(ReorderEdgeAction.ReorderType reorderType)
                    {
                        Store.Dispatch(new ReorderEdgeAction(edge.EdgeModel, reorderType));

                        // Refresh the edge bubbles
                        edge.EdgeModel.FromPort.NodeModel.RevealReorderableEdgesOrder(true, edge.EdgeModel);
                        edge.EdgeModel.FromPort.NodeModel.GetUI<Node>(GraphView)?.UpdateOutgoingExecutionEdges();
                    }
                }
            }
        }
    }
}
