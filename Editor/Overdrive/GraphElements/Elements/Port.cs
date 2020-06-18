using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class Port : VisualElementBridge, IGraphElement
    {
        public GraphElementPartList PartList { get; private set; }

        public GraphView GraphView { get; private set; }
        public IGTFPortModel PortModel { get; private set; }
        public IGTFGraphElementModel Model => PortModel;
        public IStore Store { get; private set; }

        protected ContextualMenuManipulator m_ContextualMenuManipulator;

        public Orientation Orientation { get; set; }

        public static readonly string k_UssClassName = "ge-port";
        public static readonly string k_WillConnectModifierUssClassName = k_UssClassName.WithUssModifier("will-connect");
        public static readonly string k_ConnectedModifierUssClassName = k_UssClassName.WithUssModifier("connected");
        public static readonly string k_NotConnectedModifierUssClassName = k_UssClassName.WithUssModifier("not-connected");
        public static readonly string k_InputModifierUssClassName = k_UssClassName.WithUssModifier("direction-input");
        public static readonly string k_OutputModifierUssClassName = k_UssClassName.WithUssModifier("direction-output");

        public static readonly string k_PortDataTypeClassNamePrefix = k_UssClassName.WithUssModifier("data-type-");

        public static readonly string k_ConnectorPartName = "connector-container";

        public Port()
        {
            m_ContextualMenuManipulator = new ContextualMenuManipulator(BuildContextualMenu);
            this.AddManipulator(m_ContextualMenuManipulator);

            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        public void SetupBuildAndUpdate(IGTFGraphElementModel model, IStore store, GraphView graphView)
        {
            Setup(model, store, graphView);
            BuildUI();
            UpdateFromModel();
        }

        public void Setup(IGTFGraphElementModel portModel, IStore store, GraphView graphView)
        {
            PortModel = portModel as IGTFPortModel;
            Store = store;
            GraphView = graphView;

            PartList = new GraphElementPartList();
            BuildPartList();
        }

        protected virtual void BuildPartList()
        {
            PartList.AppendPart(PortConnectorPart.Create(k_ConnectorPartName, Model, this, k_UssClassName));
        }

        public void BuildUI()
        {
            BuildSelfUI();

            foreach (var component in PartList)
            {
                component.BuildUI(this);
            }

            foreach (var component in PartList)
            {
                component.PostBuildUI();
            }

            PostBuildUI();
        }

        protected virtual void BuildSelfUI()
        {
        }

        protected virtual void PostBuildUI()
        {
            var connectorElement = this.Q(PortConnectorPart.k_ConnectorUssName) ?? this.Q(k_ConnectorPartName) ?? this;
            EdgeConnector = new EdgeConnector(Store, GraphView, new EdgeConnectorListener());
            connectorElement.AddManipulator(EdgeConnector);

            AddToClassList(k_UssClassName);
            this.AddStylesheet("Port.uss");
        }

        public void UpdateFromModel()
        {
            UpdateSelfFromModel();

            foreach (var component in PartList)
            {
                component.UpdateFromModel();
            }
        }

        protected virtual void UpdateSelfFromModel()
        {
            EnableInClassList(k_ConnectedModifierUssClassName, PortModel.IsConnected);
            EnableInClassList(k_NotConnectedModifierUssClassName, !PortModel.IsConnected);

            EnableInClassList(k_InputModifierUssClassName, PortModel.Direction == Direction.Input);
            EnableInClassList(k_OutputModifierUssClassName, PortModel.Direction == Direction.Output);

            this.PrefixRemoveFromClassList(k_PortDataTypeClassNamePrefix);
            AddToClassList(GetClassNameForDataType(PortModel.PortDataType));

            tooltip = PortModel.ToolTip;
        }

        protected virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt) {}

        CustomStyleProperty<Color> m_PortColorProperty = new CustomStyleProperty<Color>("--port-color");
        void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            Color portColorValue = Color.clear;

            ICustomStyle customStyle = e.customStyle;
            if (customStyle.TryGetValue(m_PortColorProperty, out portColorValue))
                PortColor = portColorValue;
        }

        static string GetClassNameForDataType(Type thisPortType)
        {
            if (thisPortType == null)
                return String.Empty;

            if (thisPortType.IsSubclassOf(typeof(Component)))
                return k_PortDataTypeClassNamePrefix + "component";
            if (thisPortType.IsSubclassOf(typeof(GameObject)))
                return k_PortDataTypeClassNamePrefix + "game-object";
            if (thisPortType.IsSubclassOf(typeof(Rigidbody)) || thisPortType.IsSubclassOf(typeof(Rigidbody2D)))
                return k_PortDataTypeClassNamePrefix + "rigidbody";
            if (thisPortType.IsSubclassOf(typeof(Transform)))
                return k_PortDataTypeClassNamePrefix + "transform";
            if (thisPortType.IsSubclassOf(typeof(Texture)) || thisPortType.IsSubclassOf(typeof(Texture2D)))
                return k_PortDataTypeClassNamePrefix + "texture2d";
            if (thisPortType.IsSubclassOf(typeof(KeyCode)))
                return k_PortDataTypeClassNamePrefix + "key-code";
            if (thisPortType.IsSubclassOf(typeof(Material)))
                return k_PortDataTypeClassNamePrefix + "material";
            if (thisPortType == typeof(Object))
                return k_PortDataTypeClassNamePrefix + "object";
            return k_PortDataTypeClassNamePrefix + thisPortType.Name.ToKebabCase();
        }

        public EdgeConnector EdgeConnector { get; protected set; }

        public bool WillConnect
        {
            set => EnableInClassList(k_WillConnectModifierUssClassName, value);
        }

        public bool Highlighted
        {
            set
            {
                EnableInClassList("ge-port--highlighted", value);
                foreach (var edgeModel in PortModel.ConnectedEdges)
                {
                    var edge = edgeModel.GetUI<Edge>(GraphView);
                    edge?.UpdateFromModel();
                }
            }
        }

        Node m_Node;

        public Node node
        {
            get
            {
                var nodeModel = PortModel.NodeModel;
                return nodeModel.GetUI<Node>(GraphView);
            }
        }

        public Vector3 GetGlobalCenter()
        {
            Vector2 overriddenPosition;

            if (GraphView != null && GraphView.GetPortCenterOverride(this, out overriddenPosition))
            {
                return overriddenPosition;
            }

            var portConnector = PartList.GetPart(k_ConnectorPartName) as PortConnectorPart;
            var connector = portConnector?.Root.Q(PortConnectorPart.k_ConnectorUssName) ?? portConnector?.Root ?? this;
            return connector.LocalToWorld(connector.GetRect().center);
        }

        public Color PortColor { get; private set; }
    }
}
