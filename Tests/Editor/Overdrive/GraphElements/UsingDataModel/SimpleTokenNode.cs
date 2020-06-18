#if DISABLE_SIMPLE_MATH_TESTS
using System;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.GraphElements;
using Unity.GraphToolsFoundation.Model;
using Unity.GraphToolsFoundation.Runtime.Model;
using Unity.GraphToolsFoundations.Bridge;
using UnityEditor.VisualScripting.GraphViewModel;

namespace Editor.UsingDataModel.NoPresenters
{
    class SimpleTokenNode : TokenNode
    {
        IconBadge m_ErrorBadge;

        public IMathBookFieldNode modelNode { get; private set; }

        public SimpleTokenNode(IMathBookFieldNode node) : base(node.direction == MathBookField.Direction.Output ? CreatePort(Orientation.Horizontal, Direction.Input, PortCapacity.Multi, typeof(float)) : null
                                                               , node.direction == MathBookField.Direction.Input ? CreatePort(Orientation.Horizontal, Direction.Output, PortCapacity.Multi, typeof(float)) : null)
        {
            modelNode = node;
            userData = node;
            if (input != null)
            {
                input.userData = node;
            }
            if (output != null)
            {
                output.userData = node;
            }

            UpdateData();
            node.changed += e => UpdateData();

            RegisterCallback<AttachToPanelEvent>(e => UpdateData());
        }

        void UpdateData()
        {
            title = modelNode.fieldName;

            if (modelNode.field == null)
            {
                if (panel != null)
                {
                    if (m_ErrorBadge == null)
                        m_ErrorBadge = IconBadge.CreateError("Field not found");

                    if (m_ErrorBadge.parent == null)
                    {
                        parent.Add(m_ErrorBadge);
                        m_ErrorBadge.AttachTo(this, SpriteAlignment.TopCenter);
                    }
                }
                icon = null;
            }
            else
            {
                icon = modelNode.field.exposed ? GraphViewStaticBridge.LoadIconRequired("GraphView/Nodes/BlackboardFieldExposed.png") : null;

                if (m_ErrorBadge == null)
                    m_ErrorBadge = IconBadge.CreateError("Field not found");

                if (m_ErrorBadge != null && m_ErrorBadge.parent != null)
                {
                    m_ErrorBadge.Detach();
                    m_ErrorBadge.RemoveFromHierarchy();
                }
            }
        }

        static Port CreatePort(Orientation o, Direction d, PortCapacity c, Type t)
        {
            Port port = Port.Create<SimpleEdge>(o, d, c, t);

            port.portName = "";

            return port;
        }
    }
}
#endif
