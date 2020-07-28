using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class PlacematContainer : GraphView.Layer
    {
        public enum CycleDirection
        {
            Up,
            Down
        }

        GraphView m_GraphView;

        LinkedList<Placemat> m_Placemats;

        public IEnumerable<Placemat> Placemats => m_Placemats;

        public static int PlacematsLayer => Int32.MinValue;

        public PlacematContainer(GraphView graphView)
        {
            m_Placemats = new LinkedList<Placemat>();
            m_GraphView = graphView;

            this.AddStylesheet("PlacematContainer.uss");
            AddToClassList("placematContainer");
            pickingMode = PickingMode.Ignore;

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            m_GraphView.GraphViewChangedCallback += OnGraphViewChange;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            // ReSharper disable once DelegateSubtraction
            m_GraphView.GraphViewChangedCallback -= OnGraphViewChange;
        }

        GraphViewChange OnGraphViewChange(GraphViewChange graphViewChange)
        {
            if (graphViewChange.elementsToRemove != null)
                foreach (Placemat placemat in graphViewChange.elementsToRemove.OfType<Placemat>())
                    RemovePlacemat(placemat);

            return graphViewChange;
        }

        public void AddPlacemat(Placemat placemat)
        {
            bool updateOrders = placemat.ZOrder == 0 || (m_Placemats.Count > 0 && placemat.ZOrder <= m_Placemats.Last.Value.ZOrder);

            m_Placemats.AddLast(placemat);
            Add(placemat);

            if (updateOrders)
                UpdateElementsOrder();
        }

        void RemovePlacemat(Placemat placemat)
        {
            placemat.RemoveFromHierarchy();
            m_Placemats.Remove(placemat);
        }

        public bool GetPortCenterOverride(Port port, out Vector2 overriddenPosition)
        {
            Node rootNode = port.PortModel.NodeModel.GetUI<Node>(port.GraphView);
            if (rootNode != null)
            {
                Node currNode;
                while ((currNode = rootNode.GetFirstAncestorOfType<Node>()) != null)
                    rootNode = currNode;

                //Find the furthest placemat containing the rootNode and that is collapsed (if any)
                Placemat placemat = m_Placemats.FirstOrDefault(p => p.Collapsed && p.WillDragNode(rootNode));

                if (placemat != null)
                    return placemat.GetPortCenterOverride(port.PortModel, out overriddenPosition);
            }

            overriddenPosition = Vector3.zero;
            return false;
        }

        public void RemoveAllPlacemats()
        {
            Clear();
            m_Placemats.Clear();
        }

        internal void CyclePlacemat(Placemat placemat, CycleDirection direction)
        {
            var node = m_Placemats.Find(placemat);
            if (node == null)
                return;

            var next = direction == CycleDirection.Up ? node.Next : node.Previous;
            if (next != null)
            {
                m_Placemats.Remove(placemat);
                if (direction == CycleDirection.Down)
                    m_Placemats.AddBefore(next, node);
                else
                    m_Placemats.AddAfter(next, node);
            }

            UpdateElementsOrder();
        }

        void UpdateElementsOrder()
        {
            List<int> newOrders = new List<int>();
            List<IGTFPlacematModel> changedPlacemats = new List<IGTFPlacematModel>();

            // Reset ZOrder from placemat order in array
            int idx = 1;
            foreach (var placemat in m_Placemats)
            {
                if (placemat.ZOrder != idx)
                {
                    newOrders.Add(idx);
                    changedPlacemats.Add(placemat.PlacematModel);
                }

                idx++;
            }

            m_GraphView.Store.Dispatch(
                new ChangePlacematZOrdersAction(newOrders.ToArray(), changedPlacemats.ToArray()));

            Sort((a, b) => ((Placemat)a).ZOrder.CompareTo(((Placemat)b).ZOrder));
        }

        internal void SendToBack(Placemat placemat)
        {
            m_Placemats.Remove(placemat);
            m_Placemats.AddFirst(placemat);

            UpdateElementsOrder();
        }

        internal void BringToFront(Placemat placemat)
        {
            m_Placemats.Remove(placemat);
            m_Placemats.AddLast(placemat);

            UpdateElementsOrder();
        }
    }
}
