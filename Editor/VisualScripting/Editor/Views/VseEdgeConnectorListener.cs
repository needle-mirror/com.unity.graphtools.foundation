using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    class VseEdgeConnectorListener : IEdgeConnectorListener
    {
        readonly Action<Experimental.GraphView.Edge, Vector2> m_OnDropOutsideDelegate;
        readonly Action<Experimental.GraphView.Edge> m_OnDropDelegate;

        public VseEdgeConnectorListener(Action<Experimental.GraphView.Edge, Vector2> onDropOutsideDelegate, Action<Experimental.GraphView.Edge> onDropDelegate)
        {
            m_OnDropOutsideDelegate = onDropOutsideDelegate;
            m_OnDropDelegate = onDropDelegate;
        }

        public void OnDropOutsidePort(Experimental.GraphView.Edge edge, Vector2 position)
        {
            m_OnDropOutsideDelegate(edge, position);
        }

        public void OnDrop(GraphView graphView, Experimental.GraphView.Edge edge)
        {
            m_OnDropDelegate(edge);
        }
    }
}
