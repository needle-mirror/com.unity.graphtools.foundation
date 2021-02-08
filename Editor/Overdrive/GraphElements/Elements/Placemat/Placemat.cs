using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class Placemat : GraphElement, IResizableGraphElement
    {
        public enum MinSizePolicy
        {
            EnsureMinSize,
            DoNotEnsureMinSize
        }

        internal static readonly Vector2 defaultCollapsedSize = new Vector2(200, 42);
        static readonly int k_SelectRectOffset = 3;

        public new static readonly string ussClassName = "ge-placemat";
        public static readonly string collapsedModifierUssClassName = ussClassName.WithUssModifier("collapsed");
        public static readonly string selectionBorderElementName = "selection-border";
        public static readonly string titleContainerPartName = "title-container";
        public static readonly string collapseButtonPartName = "collapse-button";
        public static readonly string resizerPartName = "resizer";

        internal static readonly float bounds = 9.0f;
        internal static readonly float boundTop = 29.0f; // Current height of Title

        // The next two values need to be the same as USS... however, we can't get the values from there as we need them in a static
        // methods used to create new placemats
        static readonly float k_MinWidth = 200;
        static readonly float k_MinHeight = 100;

        VisualElement m_ContentContainer;

        PlacematContainer m_PlacematContainer;

        HashSet<GraphElement> m_CollapsedElements  = new HashSet<GraphElement>();

        public IPlacematModel PlacematModel => Model as IPlacematModel;

        public override VisualElement contentContainer => m_ContentContainer ?? this;

        public int ZOrder => PlacematModel.ZOrder;

        internal bool Collapsed => PlacematModel.Collapsed;

        Vector2 UncollapsedSize => PlacematModel.PositionAndSize.size;

        Vector2 CollapsedSize
        {
            get
            {
                var actualCollapsedSize = defaultCollapsedSize;
                if (UncollapsedSize.x < defaultCollapsedSize.x)
                    actualCollapsedSize.x = UncollapsedSize.x;

                return actualCollapsedSize;
            }
        }

        Rect ExpandedPosition => Collapsed ? new Rect(layout.position, UncollapsedSize) : layout;

        PlacematContainer Container =>
            m_PlacematContainer ?? (m_PlacematContainer = GetFirstAncestorOfType<PlacematContainer>());

        IEnumerable<GraphElement> CollapsedElements
        {
            get => m_CollapsedElements;
            set
            {
                foreach (var collapsedElement in m_CollapsedElements)
                {
                    collapsedElement.style.visibility = StyleKeyword.Null;
                }

                m_CollapsedElements.Clear();

                if (value == null)
                    return;

                foreach (var collapsedElement in value)
                {
                    collapsedElement.style.visibility = Visibility.Hidden;
                    m_CollapsedElements.Add(collapsedElement);
                }
            }
        }

        public Placemat()
        {
            focusable = true;

            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        protected override void BuildPartList()
        {
            var editableTitlePart = EditableTitlePart.Create(titleContainerPartName, Model, this, ussClassName);
            PartList.AppendPart(editableTitlePart);
            var collapseButtonPart = CollapseButtonPart.Create(collapseButtonPartName, Model, this, ussClassName);
            editableTitlePart.PartList.AppendPart(collapseButtonPart);
            PartList.AppendPart(FourWayResizerPart.Create(resizerPartName, Model, this, ussClassName));
        }

        protected override void BuildElementUI()
        {
            var selectionBorder = new SelectionBorder { name = selectionBorderElementName };
            selectionBorder.AddToClassList(ussClassName.WithUssElement(selectionBorderElementName));
            Add(selectionBorder);
            m_ContentContainer = selectionBorder.ContentContainer;

            base.BuildElementUI();

            usageHints = UsageHints.DynamicTransform;
            AddToClassList(ussClassName);

            // PF: Fix this: Placemats are automatically added whereas other elements need to be added manually
            // with GraphView.AddElement. Furthermore, calling GraphView.AddElement(placemat) will remove it
            // from the placemat container and add it to the layer 0.
            GraphView.PlacematContainer.AddPlacemat(this);
        }

        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            var collapseButton = this.Q(collapseButtonPartName);
            collapseButton?.RegisterCallback<ChangeEvent<bool>>(OnCollapseChangeEvent);

            this.AddStylesheet("Placemat.uss");
        }

        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            style.backgroundColor = PlacematModel.Color;

            var newPos = PlacematModel.PositionAndSize.position;
            style.left = newPos.x;
            style.top = newPos.y;

            CollapseSelf();

            if (PlacematModel.Collapsed)
            {
                var collapsedElements = new List<GraphElement>();
                if (PlacematModel.HiddenElements != null)
                {
                    foreach (var elementModel in PlacematModel.HiddenElements)
                    {
                        var graphElement = elementModel.GetUI<GraphElement>(GraphView);
                        if (graphElement != null)
                            collapsedElements.Add(graphElement);
                    }
                }

                GatherCollapsedEdges(collapsedElements);
                CollapsedElements = collapsedElements;
            }
            else
            {
                CollapsedElements = null;
            }
        }

        // PF FIXME: we can probably improve the performance of this.
        // Idea: build a bounding box of placemats affected by currentPlacemat and use this BB to intersect with nodes.
        // PF TODO: also revisit Placemat other recursive functions for perf improvements.
        static void GatherDependencies(Placemat currentPlacemat, IList<GraphElement> graphElements, ICollection<GraphElement> dependencies)
        {
            if (currentPlacemat.PlacematModel.Collapsed)
            {
                foreach (var cge in currentPlacemat.CollapsedElements)
                {
                    dependencies.Add(cge);
                    if (cge is Placemat placemat)
                        GatherDependencies(placemat, graphElements, dependencies);
                }

                return;
            }

            // We want gathering dependencies to work even if the placemat layout is not up to date, so we use the
            // currentPlacemat.PlacematModel.PositionAndSize to do our overlap test.
            var currRect = currentPlacemat.PlacematModel.PositionAndSize;
            var currentActivePlacematRect = new Rect(
                currRect.x + k_SelectRectOffset,
                currRect.y + k_SelectRectOffset,
                currRect.width - 2 * k_SelectRectOffset,
                currRect.height - 2 * k_SelectRectOffset);

            foreach (var elem in graphElements)
            {
                if (elem.layout.Overlaps(currentActivePlacematRect))
                {
                    var placemat = elem as Placemat;
                    if (placemat != null && placemat.ZOrder > currentPlacemat.ZOrder)
                    {
                        GatherDependencies(placemat, graphElements, dependencies);
                    }

                    if (placemat == null || placemat.ZOrder > currentPlacemat.ZOrder)
                        dependencies.Add(elem);
                }
            }
        }

        public override void AddForwardDependencies()
        {
            var graphElements = GraphView.GraphElements.ToList()
                .Where(e => !(e is Edge) && (e.parent is GraphView.Layer) && e.IsSelectable())
                .ToList();

            var dependencies = new List<GraphElement>();
            GatherDependencies(this, graphElements, dependencies);
            var nodeModels = dependencies.Select(e => e.Model).OfType<INodeModel>();
            foreach (var edgeModel in nodeModels.SelectMany(n => n.GetConnectedEdges()))
            {
                var ui = edgeModel.GetUI(GraphView);
                if (ui != null)
                {
                    // Edge models endpoints need to be updated.
                    Dependencies.AddForwardDependency(ui, DependencyType.Geometry | DependencyType.Removal);
                }
            }
        }

        void OnCollapseChangeEvent(ChangeEvent<bool> evt)
        {
            SetCollapsed(evt.newValue);
        }

        public override void SetPosition(Rect newPos)
        {
            if (Collapsed)
                newPos.size = CollapsedSize;

            base.SetPosition(newPos);
            style.height = newPos.height;
            style.width = newPos.width;
        }

        void CollapseSelf()
        {
            if (Collapsed)
            {
                style.width = CollapsedSize.x;
                style.height = CollapsedSize.y;
            }
            else
            {
                style.width = UncollapsedSize.x;
                style.height = UncollapsedSize.y;
            }
            EnableInClassList(collapsedModifierUssClassName, Collapsed);
        }

        static IEnumerable<GraphElement> AllCollapsedElements(IEnumerable<GraphElement> collapsedElements)
        {
            if (collapsedElements != null)
            {
                foreach (var element in collapsedElements)
                {
                    switch (element)
                    {
                        case Placemat placemat when placemat.Collapsed:
                        {
                            // TODO: evaluate performance of this recursive call.
                            foreach (var subElement in AllCollapsedElements(placemat.CollapsedElements))
                                yield return subElement;
                            yield return element;
                            break;
                        }
                        case Placemat placemat when !placemat.Collapsed:
                            yield return element;
                            break;
                        case GraphElement e when e.IsMovable():
                            yield return element;
                            break;
                    }
                }
            }
        }

        void GatherCollapsedEdges(ICollection<GraphElement> collapsedElements)
        {
            var allCollapsedNodes = AllCollapsedElements(collapsedElements)
                .Select(e => e.Model)
                .OfType<INodeModel>()
                .ToList();
            foreach (var edge in PlacematModel.GraphModel.EdgeModels)
            {
                if (AnyNodeIsConnectedToPort(allCollapsedNodes, edge.ToPort) && AnyNodeIsConnectedToPort(allCollapsedNodes, edge.FromPort))
                {
                    var edgeUI = edge.GetUI<GraphElement>(GraphView);
                    if (!collapsedElements.Contains(edgeUI))
                    {
                        collapsedElements.Add(edgeUI);
                    }
                }
            }
        }

        List<IGraphElementModel> GatherCollapsedElements()
        {
            List<GraphElement> collapsedElements = new List<GraphElement>();

            var graphElements = GraphView.GraphElements.ToList()
                .Where(e => !(e is Edge) && (e.parent is GraphView.Layer) && e.IsSelectable())
                .ToList();

            var collapsedElementsElsewhere = new List<GraphElement>();
            RecurseGatherCollapsedElements(this, graphElements, collapsedElementsElsewhere);

            var nodes = new HashSet<INodeModel>(AllCollapsedElements(collapsedElements).Select(e => e.Model).OfType<INodeModel>());

            foreach (var edge in GraphView.Edges.ToList())
                if (AnyNodeIsConnectedToPort(nodes, edge.Input) && AnyNodeIsConnectedToPort(nodes, edge.Output))
                    collapsedElements.Add(edge);

            foreach (var ge in collapsedElementsElsewhere)
                collapsedElements.Remove(ge);

            return collapsedElements.Select(e => e.Model).ToList();

            void RecurseGatherCollapsedElements(Placemat currentPlacemat, IList<GraphElement> graphElementsParam,
                List<GraphElement> collapsedElementsElsewhereParam)
            {
                var currRect = currentPlacemat.ExpandedPosition;
                var currentActivePlacematRect = new Rect(
                    currRect.x + k_SelectRectOffset,
                    currRect.y + k_SelectRectOffset,
                    currRect.width - 2 * k_SelectRectOffset,
                    currRect.height - 2 * k_SelectRectOffset);
                foreach (var elem in graphElementsParam)
                {
                    if (elem.layout.Overlaps(currentActivePlacematRect))
                    {
                        var placemat = elem as Placemat;
                        if (placemat != null && placemat.ZOrder > currentPlacemat.ZOrder)
                        {
                            if (placemat.Collapsed)
                                foreach (var cge in placemat.CollapsedElements)
                                    collapsedElementsElsewhereParam.Add(cge);
                            else
                                RecurseGatherCollapsedElements(placemat, graphElementsParam, collapsedElementsElsewhereParam);
                        }

                        if (placemat == null || placemat.ZOrder > currentPlacemat.ZOrder)
                            if (elem.resolvedStyle.visibility == Visibility.Visible)
                                collapsedElements.Add(elem);
                    }
                }
            }
        }

        static bool AnyNodeIsConnectedToPort(IEnumerable<INodeModel> nodes, IPortModel port)
        {
            if (port.NodeModel == null)
            {
                return false;
            }

            foreach (var node in nodes)
            {
                if (node == port.NodeModel)
                    return true;
            }

            return false;
        }

        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);
            var mde = evt as PointerDownEvent;
            if (mde != null)
                if (mde.clickCount == 2 && mde.button == (int)MouseButton.LeftMouse)
                    SelectGraphElementsOver();
        }

        void ActOnGraphElementsOver(Action<GraphElement> act)
        {
            var graphElements = GraphView.GraphElements.ToList()
                .Where(e => !(e is Edge) && (e.parent is GraphView.Layer) && IsSelectable());

            foreach (var elem in graphElements)
            {
                if (elem.layout.Overlaps(layout))
                    act(elem);
            }
        }

        internal bool ActOnGraphElementsOver(Func<GraphElement, bool> act, bool includePlacemats)
        {
            var graphElements = GraphView.GraphElements.ToList()
                .Where(e => !(e is Edge) && e.parent is GraphView.Layer && IsSelectable()).ToList();

            return RecurseActOnGraphElementsOver_LocalFunc(this, graphElements, act, includePlacemats);
        }

        // TODO: Move to local function of ActOnGraphElementsOver once we move to C# 7.0 or higher.
        static bool RecurseActOnGraphElementsOver_LocalFunc(Placemat currentPlacemat, List<GraphElement> graphElements,
            Func<GraphElement, bool> act, bool includePlacemats)
        {
            if (currentPlacemat.Collapsed)
            {
                foreach (var elem in currentPlacemat.CollapsedElements)
                {
                    var placemat = elem as Placemat;
                    if (placemat != null && placemat.ZOrder > currentPlacemat.ZOrder)
                        if (RecurseActOnGraphElementsOver_LocalFunc(placemat, graphElements, act, includePlacemats))
                            return true;

                    if (placemat == null || (includePlacemats && placemat.ZOrder > currentPlacemat.ZOrder))
                        if (act(elem))
                            return true;
                }
            }
            else
            {
                var currRect = currentPlacemat.ExpandedPosition;
                var currentActivePlacematRect = new Rect(
                    currRect.x + k_SelectRectOffset,
                    currRect.y + k_SelectRectOffset,
                    currRect.width - 2 * k_SelectRectOffset,
                    currRect.height - 2 * k_SelectRectOffset);

                foreach (var elem in graphElements)
                {
                    if (elem.layout.Overlaps(currentActivePlacematRect))
                    {
                        var placemat = elem as Placemat;
                        if (placemat != null && placemat.ZOrder > currentPlacemat.ZOrder)
                            if (RecurseActOnGraphElementsOver_LocalFunc(placemat, graphElements, act, includePlacemats))
                                return true;

                        if (placemat == null || (includePlacemats && placemat.ZOrder > currentPlacemat.ZOrder))
                            if (elem.resolvedStyle.visibility != Visibility.Hidden)
                                if (act(elem))
                                    return true;
                    }
                }
            }
            return false;
        }

        void SelectGraphElementsOver()
        {
            ActOnGraphElementsOver(e => GraphView.AddToSelection(e));
        }

        internal bool WillDragNode(Node node)
        {
            if (Collapsed)
                return AllCollapsedElements(CollapsedElements).Contains(node);

            return ActOnGraphElementsOver(t => node == t, true);
        }

        internal void GrowToFitElements(List<GraphElement> elements)
        {
            if (elements == null)
                elements = GetHoveringNodes();

            var pos = new Rect();
            if (elements.Count > 0 && ComputeElementBounds(ref pos, elements, MinSizePolicy.DoNotEnsureMinSize))
            {
                // We don't resize to be snug. In other words: we don't ever decrease in size.
                Rect currentRect = GetPosition();
                if (pos.xMin > currentRect.xMin)
                    pos.xMin = currentRect.xMin;

                if (pos.xMax < currentRect.xMax)
                    pos.xMax = currentRect.xMax;

                if (pos.yMin > currentRect.yMin)
                    pos.yMin = currentRect.yMin;

                if (pos.yMax < currentRect.yMax)
                    pos.yMax = currentRect.yMax;

                MakeRectAtLeastMinimalSize(ref pos);
                CommandDispatcher.Dispatch(new ChangePlacematLayoutCommand(pos, ResizeFlags.All, PlacematModel));
            }
        }

        internal void ShrinkToFitElements(List<GraphElement> elements)
        {
            if (elements == null)
                elements = GetHoveringNodes();

            var pos = new Rect();
            if (elements.Count > 0 && ComputeElementBounds(ref pos, elements))
                CommandDispatcher.Dispatch(new ChangePlacematLayoutCommand(pos, ResizeFlags.All, PlacematModel));
        }

        void ResizeToIncludeSelectedNodes()
        {
            List<GraphElement> nodes = GraphView.Selection.OfType<GraphElement>().Where(e => e is Node).ToList();

            // Now include the selected nodes
            var pos = new Rect();
            if (ComputeElementBounds(ref pos, nodes, MinSizePolicy.DoNotEnsureMinSize))
            {
                // We don't resize to be snug: we only resize enough to contain the selected nodes.
                var currentRect = GetPosition();
                if (pos.xMin > currentRect.xMin)
                    pos.xMin = currentRect.xMin;

                if (pos.xMax < currentRect.xMax)
                    pos.xMax = currentRect.xMax;

                if (pos.yMin > currentRect.yMin)
                    pos.yMin = currentRect.yMin;

                if (pos.yMax < currentRect.yMax)
                    pos.yMax = currentRect.yMax;

                MakeRectAtLeastMinimalSize(ref pos);

                CommandDispatcher.Dispatch(new ChangePlacematLayoutCommand(pos, ResizeFlags.All, PlacematModel));
            }
        }

        internal void GetElementsToMove(bool moveOnlyPlacemat, HashSet<GraphElement> collectedElementsToMove)
        {
            if (Collapsed)
            {
                var collapsedElements = AllCollapsedElements(CollapsedElements);
                foreach (var element in collapsedElements)
                {
                    collectedElementsToMove.Add(element);
                }
            }
            else if (!moveOnlyPlacemat)
            {
                ActOnGraphElementsOver(e =>
                {
                    collectedElementsToMove.Add(e);
                    return false;
                }, true);
            }
        }

        public void OnResized(Rect newRect, ResizeFlags resizeWhat)
        {
            CommandDispatcher.Dispatch(new ChangePlacematLayoutCommand(newRect, resizeWhat, PlacematModel));
        }

        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            if (!(evt.currentTarget is Placemat placemat))
                return;

            if (evt.menu.MenuItems().Count > 0)
                evt.menu.AppendSeparator();

            evt.menu.AppendAction(placemat.Collapsed ? "Expand Placemat" : "Collapse Placemat",
                a => placemat.SetCollapsed(!placemat.Collapsed));

            // Gather nodes here so that we don't recycle this code in the resize functions.
            List<GraphElement> hoveringNodes = placemat.GetHoveringNodes();

            evt.menu.AppendAction("Resize Placemat/Grow to Fit",
                a => placemat.GrowToFitElements(hoveringNodes),
                hoveringNodes.Count > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Resize Placemat/Shrink to Fit",
                a => placemat.ShrinkToFitElements(hoveringNodes),
                hoveringNodes.Count > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Resize Placemat/Grow to Fit Selection",
                a => placemat.ResizeToIncludeSelectedNodes(),
                s =>
                {
                    if (placemat.GraphView.Selection.OfType<Node>().Any(n => !hoveringNodes.Contains(n)))
                        return DropdownMenuAction.Status.Normal;

                    return DropdownMenuAction.Status.Disabled;
                });

            var placematIsTop = placemat.Container.Placemats.Last() == placemat;
            var placematIsBottom = placemat.Container.Placemats.First() == placemat;
            var canBeReordered = placemat.Container.Placemats.Count > 1;

            evt.menu.AppendAction("Reorder Placemat/Bring to Front",
                a => placemat.Container.BringToFront(placemat),
                canBeReordered && !placematIsTop ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendAction("Reorder Placemat/Bring Forward",
                a => placemat.Container.CyclePlacemat(placemat, PlacematContainer.CycleDirection.Up),
                canBeReordered && !placematIsTop ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendAction("Reorder Placemat/Send Backward",
                a => placemat.Container.CyclePlacemat(placemat, PlacematContainer.CycleDirection.Down),
                canBeReordered && !placematIsBottom ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendAction("Reorder Placemat/Send to Back",
                a => placemat.Container.SendToBack(placemat),
                canBeReordered && !placematIsBottom ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        List<GraphElement> GetHoveringNodes()
        {
            var potentialElements = new List<GraphElement>();
            ActOnGraphElementsOver(e => potentialElements.Add(e));

            return potentialElements.Where(e => e is Node).ToList();
        }

        internal void SetCollapsed(bool value)
        {
            var collapsedModels = value ? GatherCollapsedElements() : null;
            CommandDispatcher.Dispatch(new SetPlacematCollapsedCommand(PlacematModel, value, collapsedModels));
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            CollapsedElements = null;
        }

        internal bool GetPortCenterOverride(IPortModel port, out Vector2 overriddenPosition)
        {
            if (!Collapsed || parent == null)
            {
                overriddenPosition = Vector2.zero;
                return false;
            }

            const int xOffset = 6;
            const int yOffset = 3;
            var halfSize = CollapsedSize * 0.5f;
            var offset = port.Orientation == Orientation.Horizontal
                ? new Vector2(port.Direction == Direction.Input ? -halfSize.x + xOffset : halfSize.x - xOffset, 0)
                : new Vector2(0, port.Direction == Direction.Input ? -halfSize.y + yOffset : halfSize.y - yOffset);

            overriddenPosition = parent.LocalToWorld(layout.center + offset);
            return true;
        }

        // Helper method that calculates how big a Placemat should be to fit the nodes on top of it currently.
        // Returns false if bounds could not be computed.
        public static bool ComputeElementBounds(ref Rect pos, List<GraphElement> elements, MinSizePolicy ensureMinSize = MinSizePolicy.EnsureMinSize)
        {
            if (elements == null || elements.Count == 0)
                return false;

            float minX =  Mathf.Infinity;
            float maxX = -Mathf.Infinity;
            float minY =  Mathf.Infinity;
            float maxY = -Mathf.Infinity;

            foreach (var r in elements.Select(n => n.GetPosition()))
            {
                if (r.xMin < minX)
                    minX = r.xMin;

                if (r.xMax > maxX)
                    maxX = r.xMax;

                if (r.yMin < minY)
                    minY = r.yMin;

                if (r.yMax > maxY)
                    maxY = r.yMax;
            }

            var width = maxX - minX + bounds * 2.0f;
            var height = maxY - minY + bounds * 2.0f + boundTop;

            pos = new Rect(
                minX - bounds,
                minY - (boundTop + bounds),
                width,
                height);

            if (ensureMinSize == MinSizePolicy.EnsureMinSize)
                MakeRectAtLeastMinimalSize(ref pos);

            return true;
        }

        static void MakeRectAtLeastMinimalSize(ref Rect r)
        {
            if (r.width < k_MinWidth)
                r.width = k_MinWidth;

            if (r.height < k_MinHeight)
                r.height = k_MinHeight;
        }
    }
}
