using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.InternalModels;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
// ReSharper disable InconsistentNaming

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// UI for a <see cref="IPortModel"/>.
    /// Allows connection of <see cref="Edge"/>s.
    /// Handles dropping of elements on top of them to create an edge.
    /// </summary>
    public class Port : DropTarget
    {
        public static readonly string ussClassName = "ge-port";
        public static readonly string highlightedModifierUssClassName = ussClassName.WithUssModifier("highlighted");
        public static readonly string willConnectModifierUssClassName = ussClassName.WithUssModifier("will-connect");
        public static readonly string connectedModifierUssClassName = ussClassName.WithUssModifier("connected");
        public static readonly string notConnectedModifierUssClassName = ussClassName.WithUssModifier("not-connected");
        public static readonly string inputModifierUssClassName = ussClassName.WithUssModifier("direction-input");
        public static readonly string outputModifierUssClassName = ussClassName.WithUssModifier("direction-output");
        public static readonly string hiddenModifierUssClassName = ussClassName.WithUssModifier("hidden");
        public static readonly string dropHighlightAcceptedClass = ussClassName.WithUssModifier("drop-highlighted");
        public static readonly string dropHighlightDeniedClass = dropHighlightAcceptedClass.WithUssModifier("denied");
        public static readonly string portDataTypeClassNamePrefix = ussClassName.WithUssModifier("data-type-");
        public static readonly string portTypeModifierClassNamePrefix = ussClassName.WithUssModifier("type-");

        /// <summary>
        /// The USS class name used for vertical ports.
        /// </summary>
        public static readonly string verticalModifierUssClassName = ussClassName.WithUssModifier("vertical");

        public static readonly string connectorPartName = "connector-container";
        public static readonly string constantEditorPartName = "constant-editor";

        CustomStyleProperty<Color> m_PortColorProperty = new CustomStyleProperty<Color>("--port-color");

        EdgeConnector m_EdgeConnector;

        public IPortModel PortModel => Model as IPortModel;

        public EdgeConnector EdgeConnector
        {
            get => m_EdgeConnector;
            protected set
            {
                var connectorElement = this.SafeQ(PortConnectorPart.connectorUssName) ?? this.SafeQ(connectorPartName) ?? this;
                connectorElement.ReplaceManipulator(ref m_EdgeConnector, value);
            }
        }

        public bool WillConnect
        {
            set => EnableInClassList(willConnectModifierUssClassName, value);
        }

        public bool Highlighted
        {
            set
            {
                EnableInClassList(highlightedModifierUssClassName, value);
                foreach (var edgeModel in PortModel.GetConnectedEdges())
                {
                    var edge = edgeModel.GetUI<Edge>(GraphView);
                    edge?.UpdateFromModel();
                }
            }
        }

        public Color PortColor { get; private set; }

        string m_CurrentDropHighlightClass = dropHighlightAcceptedClass;

        public Port()
        {
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        public override bool CanAcceptSelectionDrop(IReadOnlyList<IGraphElementModel> dragSelection)
        {
            return dragSelection.Count == 1
                && PortModel.PortType == PortType.Data
                && HasModelToDrop(dragSelection[0]);
        }

        protected override void OnDragEnd()
        {
            base.OnDragEnd();
            RemoveFromClassList(m_CurrentDropHighlightClass);
        }

        public override void OnDragEnter(DragEnterEvent evt)
        {
            base.OnDragEnter(evt);
            m_CurrentDropHighlightClass = CurrentDropAccepted ? dropHighlightAcceptedClass : dropHighlightDeniedClass;
            AddToClassList(m_CurrentDropHighlightClass);
        }

        public override void OnDragPerform(DragPerformEvent evt)
        {
            base.OnDragPerform(evt);

            if (GraphView == null)
                return;

            var selectable = GraphView.GetSelection().Single(); // we already check earlier that we only have one

            if (selectable is IVariableDeclarationModel variable)
                OnDropVariableDeclarationModel(variable, evt.mousePosition);
            else
                OnDropModel(selectable);
        }

        protected override void BuildPartList()
        {
            PartList.AppendPart(PortConnectorWithIconPart.Create(connectorPartName, Model, this, ussClassName));
            PartList.AppendPart(PortConstantEditorPart.Create(constantEditorPartName, Model, this, ussClassName));
        }

        protected override void PostBuildUI()
        {
            EdgeConnector = new EdgeConnector(CommandDispatcher, GraphView, new EdgeConnectorListener());
            EdgeConnector.SetDropOutsideDelegate(OnDropOutsideCallback);

            AddToClassList(ussClassName);
            this.AddStylesheet("Port.uss");
        }

        public override void AddModelDependencies()
        {
            if (PortModel.IsConnected())
            {
                foreach (var edgeModel in PortModel.GetConnectedEdges())
                {
                    Dependencies.AddModelDependency(edgeModel);
                }
            }
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(m_PortColorProperty, out var portColorValue))
                PortColor = portColorValue;

            if (PartList.GetPart(connectorPartName) is PortConnectorWithIconPart portConnector)
            {
                portConnector.UpdateFromModel();
            }
        }

        static string GetClassNameModifierForType(PortType t)
        {
            return portTypeModifierClassNamePrefix + t.ToString().ToLower();
        }

        /// <inheritdoc />
        protected override void UpdateElementFromModel()
        {
            var hidden = PortModel?.Options.HasFlag(PortModelOptions.Hidden) ?? false;
            EnableInClassList(hiddenModifierUssClassName, hidden);

            EnableInClassList(connectedModifierUssClassName, PortModel.IsConnected());
            EnableInClassList(notConnectedModifierUssClassName, !PortModel.IsConnected());

            EnableInClassList(inputModifierUssClassName, PortModel.Direction == Direction.Input);
            EnableInClassList(outputModifierUssClassName, PortModel.Direction == Direction.Output);

            EnableInClassList(verticalModifierUssClassName, PortModel.Orientation == Orientation.Vertical);

            this.PrefixRemoveFromClassList(portDataTypeClassNamePrefix);
            AddToClassList(GetClassNameForDataType(PortModel.PortDataType));

            this.PrefixRemoveFromClassList(portTypeModifierClassNamePrefix);
            AddToClassList(GetClassNameModifierForType(PortModel.PortType));

            tooltip = PortModel.Orientation == Orientation.Horizontal ? PortModel.ToolTip :
                string.IsNullOrEmpty(PortModel.ToolTip) ? PortModel.UniqueName :
                PortModel.UniqueName + "\n" + PortModel.ToolTip;
        }

        static string GetClassNameForDataType(Type thisPortType)
        {
            if (thisPortType == null)
                return String.Empty;

            if (thisPortType.IsSubclassOf(typeof(Component)))
                return portDataTypeClassNamePrefix + "component";
            if (thisPortType.IsSubclassOf(typeof(GameObject)))
                return portDataTypeClassNamePrefix + "game-object";
            if (thisPortType.IsSubclassOf(typeof(Rigidbody)) || thisPortType.IsSubclassOf(typeof(Rigidbody2D)))
                return portDataTypeClassNamePrefix + "rigidbody";
            if (thisPortType.IsSubclassOf(typeof(Transform)))
                return portDataTypeClassNamePrefix + "transform";
            if (thisPortType.IsSubclassOf(typeof(Texture)) || thisPortType.IsSubclassOf(typeof(Texture2D)))
                return portDataTypeClassNamePrefix + "texture2d";
            if (thisPortType.IsSubclassOf(typeof(KeyCode)))
                return portDataTypeClassNamePrefix + "key-code";
            if (thisPortType.IsSubclassOf(typeof(Material)))
                return portDataTypeClassNamePrefix + "material";
            if (thisPortType == typeof(Object))
                return portDataTypeClassNamePrefix + "object";
            return portDataTypeClassNamePrefix + thisPortType.Name.ToKebabCase();
        }

        public Vector3 GetGlobalCenter()
        {
            Vector2 overriddenPosition;

            if (GraphView != null && GraphView.GetPortCenterOverride(this, out overriddenPosition))
            {
                return overriddenPosition;
            }

            var connector = GetConnector();
            return connector.LocalToWorld(connector.GetRect().center);
        }

        public VisualElement GetConnector()
        {
            var portConnector = PartList.GetPart(connectorPartName) as PortConnectorPart;
            var connector = portConnector?.Root.SafeQ(PortConnectorPart.connectorUssName) ?? portConnector?.Root ?? this;
            return connector;
        }

        void OnDropOutsideCallback(CommandDispatcher commandDispatcher, IEnumerable<Edge> edges, IEnumerable<IPortModel> ports, Vector2 pos)
        {
            if (commandDispatcher.GraphToolState?.WindowState.GraphModel?.Stencil == null)
                return;

            Vector2 localPos = GraphView.ContentViewContainer.WorldToLocal(pos);

            List<IEdgeModel> edgesToDelete = new List<IEdgeModel>();
            List<IPortModel> existingPortModels = new List<IPortModel>();

            foreach (var edge in edges.Zip(ports, (a, b) => new { edge = a, port = b }))
            {
                if (edge.edge != null) // edge.edge == null means we are creating a new edge not changing an existing one, so no deletion needed.
                {
                    edgesToDelete.AddRange(EdgeConnectorListener.GetDropEdgeModelsToDelete(edge.edge.EdgeModel));

                    // when grabbing an existing edge's end, the edgeModel should be deleted
                    if (edge.edge.EdgeModel != null && !(edge.edge.EdgeModel is GhostEdgeModel))
                        edgesToDelete.Add(edge.edge.EdgeModel);
                }

                existingPortModels.Add(edge.port);
            }

            commandDispatcher.GraphToolState.WindowState.GraphModel.Stencil.CreateNodesFromPort(commandDispatcher,
                existingPortModels, localPos, pos, edgesToDelete);
        }

        IPortModel GetPortToConnect(IGraphElementModel modelToDrop)
        {
            switch (modelToDrop)
            {
                case ISingleOutputPortNodeModel singleOutputPortNode when PortModel.Direction == Direction.Input:
                    return singleOutputPortNode.OutputPort;
                case ISingleInputPortNodeModel singleInputPortModelNode when PortModel.Direction == Direction.Output:
                    return singleInputPortModelNode.InputPort;
                default:
                    return null;
            }
        }

        bool HasModelToDrop(IGraphElementModel selectable)
        {
            var portToConnect = GetPortToConnect(selectable);
            return portToConnect != null && !ReferenceEquals(PortModel.NodeModel, portToConnect.NodeModel);
        }

        protected virtual void OnDropModel(IGraphElementModel droppedModel)
        {
            var portToConnect = GetPortToConnect(droppedModel);
            Assert.IsNotNull(portToConnect);

            CommandDispatcher.Dispatch(new CreateEdgeCommand(PortModel, portToConnect, portAlignment: Direction.Input));
        }

        protected virtual void OnDropVariableDeclarationModel(IVariableDeclarationModel variable, Vector2 mousePosition)
        {
            CommandDispatcher.Dispatch(new CreateVariableNodesCommand(variable, mousePosition, connectAfterCreation: PortModel, autoAlign: true));
        }
    }
}
