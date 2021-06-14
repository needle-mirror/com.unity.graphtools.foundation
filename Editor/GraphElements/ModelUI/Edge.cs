using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.InternalModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The UI for an <see cref="IEdgeModel"/>.
    /// </summary>
    public class Edge : GraphElement
    {
        public new static readonly string ussClassName = "ge-edge";
        public static readonly string editModeModifierUssClassName = ussClassName.WithUssModifier("edit-mode");
        public static readonly string ghostModifierUssClassName = ussClassName.WithUssModifier("ghost");

        public static readonly string edgeControlPartName = "edge-control";
        public static readonly string edgeBubblePartName = "edge-bubble";

        EdgeManipulator m_EdgeManipulator;

        EdgeControl m_EdgeControl;

        protected EdgeManipulator EdgeManipulator
        {
            get => m_EdgeManipulator;
            set => this.ReplaceManipulator(ref m_EdgeManipulator, value);
        }

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
                    var ui = port.GetUI<Port>(View);
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
                    var ui = port.GetUI<Port>(View);
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
                    var edgeControlPart = PartList.GetPart(edgeControlPartName);
                    m_EdgeControl = edgeControlPart?.Root as EdgeControl;
                }

                return m_EdgeControl;
            }
        }

        public IPortModel Output => EdgeModel.FromPort;

        public IPortModel Input => EdgeModel.ToPort;

        /// <inheritdoc />
        public override bool ShowInMiniMap => false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Edge"/> class.
        /// </summary>
        public Edge()
        {
            Layer = -1;

            EdgeManipulator = new EdgeManipulator();
        }

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            PartList.AppendPart(EdgeControlPart.Create(edgeControlPartName, Model, this, ussClassName));
            PartList.AppendPart(EdgeBubblePart.Create(edgeBubblePartName, Model, this, ussClassName));
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList(ussClassName);
            EnableInClassList(ghostModifierUssClassName, IsGhostEdge);
            this.AddStylesheet("Edge.uss");
        }

        /// <inheritdoc />
        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            if (EdgeModel is IEditableEdge editableEdge)
                EnableInClassList(editModeModifierUssClassName, editableEdge.EditMode);
        }

        /// <inheritdoc/>
        public override void AddBackwardDependencies()
        {
            base.AddBackwardDependencies();

            // When the ports move, the edge should be redrawn.
            AddDependencies(EdgeModel.FromPort);
            AddDependencies(EdgeModel.ToPort);

            void AddDependencies(IPortModel portModel)
            {
                if (portModel == null)
                    return;

                var ui = portModel.GetUI(View);
                if (ui != null)
                {
                    // Edge color changes with port color.
                    Dependencies.AddBackwardDependency(ui, DependencyTypes.Style);
                }

                ui = portModel.NodeModel.GetUI(View);
                if (ui != null)
                {
                    // Edge position changes with node position.
                    Dependencies.AddBackwardDependency(ui, DependencyTypes.Geometry);
                }
            }
        }

        /// <inheritdoc/>
        public override void AddModelDependencies()
        {
            var ui = EdgeModel.FromPort?.GetUI<Port>(View);
            ui?.AddDependencyToEdgeModel(EdgeModel);

            ui = EdgeModel.ToPort?.GetUI<Port>(View);
            ui?.AddDependencyToEdgeModel(EdgeModel);
        }

        /// <inheritdoc />
        public override bool Overlaps(Rect rectangle)
        {
            return EdgeControl.Overlaps(this.ChangeCoordinatesTo(EdgeControl, rectangle));
        }

        /// <inheritdoc />
        public override bool ContainsPoint(Vector2 localPoint)
        {
            return EdgeControl.ContainsPoint(this.ChangeCoordinatesTo(EdgeControl, localPoint));
        }

        /// <inheritdoc />
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
                        CommandDispatcher.Dispatch(new AddControlPointOnEdgeCommand(editableEdge, controlPointIndex, p));
                    });
                }

                evt.menu.AppendAction("Stop Editing Edge", menuAction =>
                {
                    CommandDispatcher.Dispatch(new SetEdgeEditModeCommand(editableEdge, false));
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
                        CommandDispatcher.Dispatch(new SetEdgeEditModeCommand(editableEdge, true));
                    });
                }

                if ((edge.EdgeModel.FromPort as IReorderableEdgesPortModel)?.HasReorderableEdges ?? false)
                {
                    if (!initialSeparatorAdded && initialMenuItemCount > 0)
                        evt.menu.AppendSeparator();

                    var siblingEdges = edge.EdgeModel.FromPort.GetConnectedEdges().ToList();
                    var siblingEdgesCount = siblingEdges.Count;

                    var index = siblingEdges.IndexOf(edge.EdgeModel);
                    evt.menu.AppendAction("Reorder Edge/Move First",
                        a => ReorderEdges(ReorderEdgeCommand.ReorderType.MoveFirst),
                        siblingEdgesCount > 1 && index > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                    evt.menu.AppendAction("Reorder Edge/Move Up",
                        a => ReorderEdges(ReorderEdgeCommand.ReorderType.MoveUp),
                        siblingEdgesCount > 1 && index > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                    evt.menu.AppendAction("Reorder Edge/Move Down",
                        a => ReorderEdges(ReorderEdgeCommand.ReorderType.MoveDown),
                        siblingEdgesCount > 1 && index < siblingEdgesCount - 1 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                    evt.menu.AppendAction("Reorder Edge/Move Last",
                        a => ReorderEdges(ReorderEdgeCommand.ReorderType.MoveLast),
                        siblingEdgesCount > 1 && index < siblingEdgesCount - 1 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                    void ReorderEdges(ReorderEdgeCommand.ReorderType reorderType)
                    {
                        CommandDispatcher.Dispatch(new ReorderEdgeCommand(edge.EdgeModel, reorderType));
                    }
                }
            }
        }
    }
}
