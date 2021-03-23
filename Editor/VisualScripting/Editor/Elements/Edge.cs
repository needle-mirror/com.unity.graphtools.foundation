using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    class Edge : Experimental.GraphView.Edge, IHasGraphElementModel
    {
        readonly IEdgeModel m_EdgeModel;
        readonly EdgeBubble m_EdgeBubble;

        public Edge(IEdgeModel edgeModel) : this()
        {
            m_EdgeModel = edgeModel;

            capabilities = VseUtility.ConvertCapabilities(m_EdgeModel);

            PortType portType = m_EdgeModel?.OutputPortModel?.PortType ?? PortType.Data;
            EnableInClassList("execution", portType == PortType.Execution || portType == PortType.Loop);
            EnableInClassList("event", portType == PortType.Event);
            viewDataKey = m_EdgeModel?.GetId();
        }

        // Necessary for EdgeConnector, which creates temporary edges
        public Edge()
        {
            layer = -1;

            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "Edge.uss"));

            RegisterCallback<AttachToPanelEvent>(OnTargetAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnTargetDetachedFromPanel);

            m_EdgeBubble = new EdgeBubble();
        }

        void OnTargetAttachedToPanel(AttachToPanelEvent evt)
        {
            Add(m_EdgeBubble);

            if (m_EdgeModel?.OutputPortModel != null)
                m_EdgeModel.OutputPortModel.OnValueChanged += OnPortValueChanged;
        }

        void OnTargetDetachedFromPanel(DetachFromPanelEvent evt)
        {
            if (m_EdgeModel?.OutputPortModel != null)
                // ReSharper disable once DelegateSubtraction
                m_EdgeModel.OutputPortModel.OnValueChanged -= OnPortValueChanged;

            m_EdgeBubble.Detach();
            m_EdgeBubble.RemoveFromHierarchy();
        }

        void OnPortValueChanged()
        {
            OnPortChanged(isInput: false);
        }

#if UNITY_2020_1_OR_NEWER
        public override bool UpdateEdgeControl()
        {
            schedule.Execute(_ => UpdateEdgeBubble());
            return base.UpdateEdgeControl();
        }

#endif

        public override void OnPortChanged(bool isInput)
        {
            base.OnPortChanged(isInput);

            // Function can be called on initialization from GraphView before the element is attached to a panel
            if (panel == null)
                return;

            UpdateEdgeBubble();
        }

        void UpdateEdgeBubble()
        {
            NodeModel inputPortNodeModel = m_EdgeModel?.InputPortModel?.NodeModel as NodeModel;
            NodeModel outputPortNodeModel = m_EdgeModel?.OutputPortModel?.NodeModel as NodeModel;

            PortType portType = m_EdgeModel?.OutputPortModel?.PortType ?? PortType.Data;
            if ((portType == PortType.Execution || portType == PortType.Loop) && (outputPortNodeModel != null || inputPortNodeModel != null) &&
                !string.IsNullOrEmpty(m_EdgeModel?.EdgeLabel) &&
                visible)
            {
                m_EdgeBubble.text = m_EdgeModel?.EdgeLabel;
                m_EdgeBubble.EnableInClassList("candidate", (output == null || input == null));
                m_EdgeBubble.AttachTo(edgeControl, SpriteAlignment.Center);
                m_EdgeBubble.style.visibility = StyleKeyword.Null;
            }
            else
            {
                m_EdgeBubble.Detach();
                m_EdgeBubble.style.visibility = Visibility.Hidden;
            }
        }

        public void Rename(string value)
        {
            // TODO: useful only if user can provide a direct condition via a string
            // (and this is only valid for conditional branch edges)
            // m_Store.Dispatch(new RenameEdgeAction(model, value));
        }

        public IGraphElementModel GraphElementModel => m_EdgeModel;
        public IEdgeModel model => m_EdgeModel;
    }
}
