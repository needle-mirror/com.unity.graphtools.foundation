using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class SelectionDragger : Dragger
    {
        IDropTarget m_PrevDropTarget;
        bool m_Dragging;
        readonly Snapper m_Snapper = new Snapper();

        // selectedElement is used to store a unique selection candidate for cases where user clicks on an item not to
        // drag it but just to reset the selection -- we only know this after the manipulation has ended
        GraphElement selectedElement { get; set; }
        GraphElement clickedElement { get; set; }

        private GraphViewChange m_GraphViewChange;
        private HashSet<GraphElement> m_MovedElements;
        private List<VisualElement> m_DropTargetPickList = new List<VisualElement>();
        IDropTarget GetDropTargetAt(Vector2 mousePosition, IEnumerable<VisualElement> exclusionList)
        {
            Vector2 pickPoint = mousePosition;

            List<VisualElement> pickList = m_DropTargetPickList;

            pickList.Clear();
            target.panel.PickAll(pickPoint, pickList);

            IDropTarget dropTarget = null;

            for (int i = 0; i < pickList.Count; i++)
            {
                if (pickList[i] == target && target != m_GraphView)
                    continue;

                VisualElement picked = pickList[i];

                dropTarget = picked as IDropTarget;

                if (dropTarget != null)
                {
                    foreach (var element in exclusionList)
                    {
                        if (element == picked || element.FindCommonAncestor(picked) == element)
                        {
                            dropTarget = null;
                            break;
                        }
                    }

                    if (dropTarget != null)
                        break;
                }
            }

            return dropTarget;
        }

        public SelectionDragger(GraphView graphView)
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Shift });
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Command });
            }
            else
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Control });
            }
            panSpeed = new Vector2(1, 1);
            clampToParentEdges = false;

            m_MovedElements = new HashSet<GraphElement>();
            m_GraphViewChange.movedElements = m_MovedElements;
            m_GraphView = graphView;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            var selectionContainer = target as ISelection;

            if (selectionContainer == null)
            {
                throw new InvalidOperationException("Manipulator can only be added to a control that supports selection");
            }

            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);

            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
            target.RegisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOutEvent);

            m_Dragging = false;
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);

            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            target.UnregisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOutEvent);
        }

        private GraphView m_GraphView;

        class OriginalPos
        {
            public Rect pos;
            public bool dragStarted;
        }

        private Dictionary<GraphElement, OriginalPos> m_OriginalPos;
        private Vector2 m_originalMouse;
        List<Edge> m_EdgesToUpdate = new List<Edge>();

        static void SendDragAndDropEvent(IDragAndDropEvent evt, List<ISelectableGraphElement> selection, IDropTarget dropTarget, ISelection dragSource)
        {
            if (dropTarget == null)
            {
                return;
            }

            EventBase e = evt as EventBase;
            if (e.eventTypeId == DragExitedEvent.TypeId())
            {
                dropTarget.DragExited();
            }
            else if (e.eventTypeId == DragEnterEvent.TypeId())
            {
                dropTarget.DragEnter(evt as DragEnterEvent, selection, dropTarget, dragSource);
            }
            else if (e.eventTypeId == DragLeaveEvent.TypeId())
            {
                dropTarget.DragLeave(evt as DragLeaveEvent, selection, dropTarget, dragSource);
            }

            if (!dropTarget.CanAcceptDrop(selection))
            {
                return;
            }

            if (e.eventTypeId == DragPerformEvent.TypeId())
            {
                dropTarget.DragPerform(evt as DragPerformEvent, selection, dropTarget, dragSource);
            }
            else if (e.eventTypeId == DragUpdatedEvent.TypeId())
            {
                dropTarget.DragUpdated(evt as DragUpdatedEvent, selection, dropTarget, dragSource);
            }
        }

        private void OnMouseCaptureOutEvent(MouseCaptureOutEvent e)
        {
            if (m_Active)
            {
                if (m_PrevDropTarget != null && m_GraphView != null)
                {
                    if (m_PrevDropTarget.CanAcceptDrop(m_GraphView.Selection))
                    {
                        m_PrevDropTarget.DragExited();
                    }
                }

                // Stop processing the event sequence if the target has lost focus, then.
                selectedElement = null;
                m_PrevDropTarget = null;
                m_Active = false;

                if (m_GraphView.Selection.Any())
                {
                    m_Snapper.EndSnap();
                }
            }
        }

        protected new void OnMouseDown(MouseDownEvent e)
        {
            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            if (CanStartManipulation(e))
            {
                if (m_GraphView == null)
                    return;

                selectedElement = null;

                // avoid starting a manipulation on a non movable object
                clickedElement = e.target as GraphElement;
                if (clickedElement == null)
                {
                    var ve = e.target as VisualElement;
                    clickedElement = ve.GetFirstAncestorOfType<GraphElement>();
                    if (clickedElement == null)
                        return;
                }

                // Only start manipulating if the clicked element is movable, selected and that the mouse is in its clickable region (it must be deselected otherwise).
                if (!clickedElement.IsPositioned() || !clickedElement.ContainsPoint(clickedElement.WorldToLocal(e.mousePosition)))
                    return;

                // If we hit this, this likely because the element has just been unselected
                // It is important for this manipulator to receive the event so the previous one did not stop it
                // but we shouldn't let it propagate to other manipulators to avoid a re-selection
                if (!m_GraphView.Selection.Contains(clickedElement))
                {
                    e.StopImmediatePropagation();
                    return;
                }

                selectedElement = clickedElement;

                m_OriginalPos = new Dictionary<GraphElement, OriginalPos>();

                HashSet<GraphElement> elementsToMove = new HashSet<GraphElement>(m_GraphView.Selection.OfType<GraphElement>());

                var selectedPlacemats = new HashSet<Placemat>(elementsToMove.OfType<Placemat>());
                foreach (var placemat in selectedPlacemats)
                    placemat.GetElementsToMove(e.shiftKey, elementsToMove);

                m_EdgesToUpdate.Clear();
                HashSet<IGTFNodeModel> nodeModelsToMove = new HashSet<IGTFNodeModel>(elementsToMove.Select(element => element.Model).OfType<IGTFNodeModel>());
                foreach (var edge in m_GraphView.Edges.ToList())
                {
                    if (nodeModelsToMove.Contains(edge.Input.NodeModel) && nodeModelsToMove.Contains(edge.Output.NodeModel))
                    {
                        m_EdgesToUpdate.Add(edge);
                    }
                }

                foreach (GraphElement ce in elementsToMove)
                {
                    if (ce == null || !ce.IsPositioned())
                        continue;

                    Rect geometry = ce.GetPosition();
                    Rect geometryInContentViewSpace = ce.hierarchy.parent.ChangeCoordinatesTo(m_GraphView.contentViewContainer, geometry);
                    m_OriginalPos[ce] = new OriginalPos
                    {
                        pos = geometryInContentViewSpace,
                    };
                }

                m_originalMouse = e.mousePosition;
                m_ItemPanDiff = Vector3.zero;

                if (m_PanSchedule == null)
                {
                    m_PanSchedule = m_GraphView.schedule.Execute(Pan).Every(k_PanInterval).StartingIn(k_PanInterval);
                    m_PanSchedule.Pause();
                }

                m_Snapper.BeginSnap(selectedElement);

                m_Active = true;
                target.CaptureMouse(); // We want to receive events even when mouse is not over ourself.
                e.StopImmediatePropagation();
            }
        }

        public const int k_PanAreaWidth = 100;
        public const int k_PanSpeed = 4;
        public const int k_PanInterval = 10;
        public const float k_MinSpeedFactor = 0.5f;
        public const float k_MaxSpeedFactor = 2.5f;
        public const float k_MaxPanSpeed = k_MaxSpeedFactor * k_PanSpeed;

        private IVisualElementScheduledItem m_PanSchedule;
        private Vector3 m_PanDiff = Vector3.zero;
        private Vector3 m_ItemPanDiff = Vector3.zero;
        private Vector2 m_MouseDiff = Vector2.zero;

        private float m_Scale;

        internal Vector2 GetEffectivePanSpeed(Vector2 mousePos)
        {
            Vector2 effectiveSpeed = Vector2.zero;

            if (mousePos.x <= k_PanAreaWidth)
                effectiveSpeed.x = -(((k_PanAreaWidth - mousePos.x) / k_PanAreaWidth) + 0.5f) * k_PanSpeed;
            else if (mousePos.x >= m_GraphView.contentContainer.layout.width - k_PanAreaWidth)
                effectiveSpeed.x = (((mousePos.x - (m_GraphView.contentContainer.layout.width - k_PanAreaWidth)) / k_PanAreaWidth) + 0.5f) * k_PanSpeed;

            if (mousePos.y <= k_PanAreaWidth)
                effectiveSpeed.y = -(((k_PanAreaWidth - mousePos.y) / k_PanAreaWidth) + 0.5f) * k_PanSpeed;
            else if (mousePos.y >= m_GraphView.contentContainer.layout.height - k_PanAreaWidth)
                effectiveSpeed.y = (((mousePos.y - (m_GraphView.contentContainer.layout.height - k_PanAreaWidth)) / k_PanAreaWidth) + 0.5f) * k_PanSpeed;

            effectiveSpeed = Vector2.ClampMagnitude(effectiveSpeed, k_MaxPanSpeed);

            return effectiveSpeed;
        }

        void ComputeSnappedRect(ref Rect selectedElementProposedGeom, GraphElement element)
        {
            // Check if snapping is paused first: if yes, the snapper will return the original dragging position
            if (Event.current != null)
            {
                m_Snapper.PauseSnap(Event.current.shift);
            }

            // Let the snapper compute a snapped position
            Rect geometryInContentViewContainerSpace = element.parent.ChangeCoordinatesTo(m_GraphView.contentViewContainer, selectedElementProposedGeom);

            Vector2 mousePanningDelta = new Vector2((m_MouseDiff.x - m_ItemPanDiff.x) * panSpeed.x / m_Scale, (m_MouseDiff.y - m_ItemPanDiff.y) * panSpeed.y / m_Scale);
            geometryInContentViewContainerSpace = m_Snapper.GetSnappedRect(geometryInContentViewContainerSpace, element, m_Scale, mousePanningDelta);

            // Once the snapped position is computed in the GraphView.contentViewContainer's space then
            // translate it into the local space of the parent of the selected element.
            selectedElementProposedGeom = m_GraphView.contentViewContainer.ChangeCoordinatesTo(element.parent, geometryInContentViewContainerSpace);
        }

        protected new void OnMouseMove(MouseMoveEvent e)
        {
            if (!m_Active)
                return;

            if (m_GraphView == null)
                return;

            var ve = (VisualElement)e.target;
            Vector2 gvMousePos = ve.ChangeCoordinatesTo(m_GraphView.contentContainer, e.localMousePosition);
            m_PanDiff = GetEffectivePanSpeed(gvMousePos);

            if (m_PanDiff != Vector3.zero)
            {
                m_PanSchedule.Resume();
            }
            else
            {
                m_PanSchedule.Pause();
            }

            // We need to monitor the mouse diff "by hand" because we stop positioning the graph elements once the
            // mouse has gone out.
            m_MouseDiff = m_originalMouse - e.mousePosition;

            // Handle the selected element
            Rect selectedElementGeom = GetSelectedElementGeom();

            ComputeSnappedRect(ref selectedElementGeom, selectedElement);

            foreach (KeyValuePair<GraphElement, OriginalPos> v in m_OriginalPos)
            {
                GraphElement ce = v.Key;

                // Protect against stale visual elements that have been deparented since the start of the manipulation
                if (ce.hierarchy.parent == null)
                    continue;

                if (!v.Value.dragStarted)
                {
                    v.Value.dragStarted = true;
                }
                SnapOrMoveElement(v.Key, v.Value.pos, selectedElementGeom);
            }

            foreach (var edge in m_EdgesToUpdate)
            {
                SnapOrMoveEdge(edge, selectedElementGeom);
            }

            List<ISelectableGraphElement> selection = m_GraphView.Selection;

            // TODO: Replace with a temp drawing or something...maybe manipulator could fake position
            // all this to let operation know which element sits under cursor...or is there another way to draw stuff that is being dragged?

            IDropTarget dropTarget = GetDropTargetAt(e.mousePosition, selection.OfType<VisualElement>());

            if (m_PrevDropTarget != dropTarget)
            {
                if (m_PrevDropTarget != null)
                {
                    using (DragLeaveEvent eexit = DragLeaveEvent.GetPooled(e))
                    {
                        SendDragAndDropEvent(eexit, selection, m_PrevDropTarget, m_GraphView);
                    }
                }

                using (DragEnterEvent eenter = DragEnterEvent.GetPooled(e))
                {
                    SendDragAndDropEvent(eenter, selection, dropTarget, m_GraphView);
                }
            }

            using (DragUpdatedEvent eupdated = DragUpdatedEvent.GetPooled(e))
            {
                SendDragAndDropEvent(eupdated, selection, dropTarget, m_GraphView);
            }

            m_PrevDropTarget = dropTarget;

            m_Dragging = true;
            e.StopPropagation();
        }

        private void Pan(TimerState ts)
        {
            m_GraphView.viewTransform.position -= m_PanDiff;
            m_ItemPanDiff += m_PanDiff;

            // Handle the selected element
            Rect selectedElementGeom = GetSelectedElementGeom();

            ComputeSnappedRect(ref selectedElementGeom, selectedElement);

            foreach (KeyValuePair<GraphElement, OriginalPos> v in m_OriginalPos)
            {
                SnapOrMoveElement(v.Key, v.Value.pos, selectedElementGeom);
            }
        }

        void SnapOrMoveElement(GraphElement element, Rect originalPos, Rect selectedElementGeom)
        {
            if (m_Snapper.IsActive)
            {
                Vector2 geomDiff = selectedElementGeom.position - m_OriginalPos[selectedElement].pos.position;
                Vector2 position = new Vector2(originalPos.x + geomDiff.x , originalPos.y + geomDiff.y);

                element.SetPosition(new Rect(position, element.layout.size));
            }
            else
            {
                MoveElement(element, originalPos);
            }
        }

        Rect GetSelectedElementGeom()
        {
            // Handle the selected element
            Matrix4x4 g = selectedElement.worldTransform;
            m_Scale = g.m00; //The scale on x is equal to the scale on y because the graphview is not distorted

            Rect selectedElementGeom = m_OriginalPos[selectedElement].pos;

            if (m_Snapper.IsActive)
            {
                // Compute the new position of the selected element using the mouse delta position and panning info
                selectedElementGeom.x = selectedElementGeom.x - (m_MouseDiff.x - m_ItemPanDiff.x) * panSpeed.x / m_Scale;
                selectedElementGeom.y = selectedElementGeom.y - (m_MouseDiff.y - m_ItemPanDiff.y) * panSpeed.y / m_Scale;
            }

            return selectedElementGeom;
        }

        void MoveElement(GraphElement element, Rect originalPos)
        {
            Matrix4x4 g = element.worldTransform;
            var scale = new Vector3(g.m00, g.m11, g.m22);

            Rect newPos = new Rect(0, 0, originalPos.width, originalPos.height);

            // Compute the new position of the selected element using the mouse delta position and panning info
            newPos.x = originalPos.x - (m_MouseDiff.x - m_ItemPanDiff.x) * panSpeed.x / scale.x * element.transform.scale.x;
            newPos.y = originalPos.y - (m_MouseDiff.y - m_ItemPanDiff.y) * panSpeed.y / scale.y * element.transform.scale.y;

            newPos = m_GraphView.contentViewContainer.ChangeCoordinatesTo(element.hierarchy.parent, newPos);

            element.SetPosition(newPos);
        }

        void SnapOrMoveEdge(Edge edge, Rect selectedElementGeom)
        {
            if (m_Snapper.IsActive)
            {
                Vector2 geomDiff = selectedElementGeom.position - m_OriginalPos[selectedElement].pos.position;
                var offset = new Vector2(geomDiff.x, geomDiff.y);

                edge.EdgeControl.ControlPointOffset = offset;
            }
            else
            {
                UpdateEdge(edge);
            }
        }

        void UpdateEdge(Edge edge)
        {
            Matrix4x4 g = edge.worldTransform;
            var scale = new Vector3(g.m00, g.m11, g.m22);

            // Compute the new position of the selected element using the mouse delta position and panning info
            var offset = new Vector2(
                -(m_MouseDiff.x - m_ItemPanDiff.x) * panSpeed.x / scale.x * edge.transform.scale.x,
                -(m_MouseDiff.y - m_ItemPanDiff.y) * panSpeed.y / scale.y * edge.transform.scale.y
            );

            edge.EdgeControl.ControlPointOffset = offset;
        }

        protected new void OnMouseUp(MouseUpEvent evt)
        {
            if (m_GraphView == null)
            {
                if (m_Active)
                {
                    target.ReleaseMouse();
                    selectedElement = null;
                    m_Active = false;
                    m_Dragging = false;
                    m_PrevDropTarget = null;
                }

                return;
            }

            List<ISelectableGraphElement> selection = m_GraphView.Selection;

            if (CanStopManipulation(evt))
            {
                if (m_Active)
                {
                    if (m_Dragging || selectedElement == null)
                    {
                        m_MovedElements.Clear();
                        m_MovedElements.AddRange(m_OriginalPos.Keys);
                        m_MovedElements.AddRange(m_EdgesToUpdate);

                        // PF: remove graphViewChanged and all.
                        var graphView = target as GraphView;
                        if (graphView?.GraphViewChangedCallback != null)
                        {
                            KeyValuePair<GraphElement, OriginalPos> firstPos = m_OriginalPos.First();
                            m_GraphViewChange.moveDelta = firstPos.Key.GetPosition().position - firstPos.Value.pos.position;
                            graphView.GraphViewChangedCallback(m_GraphViewChange);
                        }

                        if (m_GraphView != null)
                        {
                            KeyValuePair<GraphElement, OriginalPos> firstPos = m_OriginalPos.First();
                            var delta = firstPos.Key.GetPosition().position - firstPos.Value.pos.position;
                            var models = m_MovedElements
                                // PF remove this Where clause. It comes from VseGraphView.OnGraphViewChanged.
                                .Where(e => e is IMovableGraphElement movable && (!(e.Model is IGTFNodeModel) || movable.IsMovable))
                                .Select(e => e.Model)
                                .OfType<IPositioned>();
                            m_GraphView.Store.Dispatch(new MoveElementsAction(delta, models.ToArray()));
                        }
                        m_MovedElements.Clear();
                    }

                    foreach (var edge in m_EdgesToUpdate)
                    {
                        edge.EdgeControl.ControlPointOffset = Vector2.zero;
                    }
                    m_EdgesToUpdate.Clear();

                    m_PanSchedule.Pause();

                    if (m_ItemPanDiff != Vector3.zero)
                    {
                        Vector3 p = m_GraphView.contentViewContainer.transform.position;
                        Vector3 s = m_GraphView.contentViewContainer.transform.scale;
                        m_GraphView.UpdateViewTransform(p, s);
                    }

                    if (selection.Count > 0 && m_PrevDropTarget != null)
                    {
                        if (m_PrevDropTarget.CanAcceptDrop(selection))
                        {
                            using (DragPerformEvent drop = DragPerformEvent.GetPooled(evt))
                            {
                                SendDragAndDropEvent(drop, selection, m_PrevDropTarget, m_GraphView);
                            }
                        }
                        else
                        {
                            using (DragExitedEvent dexit = DragExitedEvent.GetPooled(evt))
                            {
                                SendDragAndDropEvent(dexit, selection, m_PrevDropTarget, m_GraphView);
                            }
                        }
                    }

                    if (selection.Any())
                    {
                        m_Snapper.EndSnap();
                    }

                    target.ReleaseMouse();
                    evt.StopPropagation();
                }
                selectedElement = null;
                m_Active = false;
                m_PrevDropTarget = null;
                m_Dragging = false;
                m_PrevDropTarget = null;
            }
        }

        private void OnKeyDown(KeyDownEvent e)
        {
            if (e.keyCode != KeyCode.Escape || m_GraphView == null || !m_Active)
                return;

            // Reset the items to their original pos.
            foreach (KeyValuePair<GraphElement, OriginalPos> v in m_OriginalPos)
            {
                OriginalPos originalPos = v.Value;
                v.Key.style.left = originalPos.pos.x;
                v.Key.style.top = originalPos.pos.y;
            }

            m_PanSchedule.Pause();

            if (m_ItemPanDiff != Vector3.zero)
            {
                Vector3 p = m_GraphView.contentViewContainer.transform.position;
                Vector3 s = m_GraphView.contentViewContainer.transform.scale;
                m_GraphView.UpdateViewTransform(p, s);
            }

            using (DragExitedEvent dexit = DragExitedEvent.GetPooled())
            {
                List<ISelectableGraphElement> selection = m_GraphView.Selection;
                SendDragAndDropEvent(dexit, selection, m_PrevDropTarget, m_GraphView);
            }

            target.ReleaseMouse();
            e.StopPropagation();
        }
    }
}
