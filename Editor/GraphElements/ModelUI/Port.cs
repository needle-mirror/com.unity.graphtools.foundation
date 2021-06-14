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

        protected CustomStyleProperty<Color> m_PortColorProperty = new CustomStyleProperty<Color>("--port-color");

        GraphView GraphView => View as GraphView;

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
                    var edge = edgeModel.GetUI<Edge>(View);
                    edge?.UpdateFromModel();
                }
            }
        }

        public Color PortColor { get; private set; }

        protected string m_CurrentDropHighlightClass = dropHighlightAcceptedClass;

        /// <summary>
        /// Initializes a new instance of the <see cref="Port"/> class.
        /// </summary>
        public Port()
        {
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        /// <inheritdoc />
        public override bool CanAcceptDrop(IReadOnlyList<IGraphElementModel> droppedElements)
        {
            return droppedElements.Count == 1
                && PortModel.PortType == PortType.Data
                && HasModelToDrop(droppedElements[0]);
        }

        /// <inheritdoc />
        protected override void OnDragEnd()
        {
            base.OnDragEnd();
            RemoveFromClassList(m_CurrentDropHighlightClass);
        }

        /// <inheritdoc />
        public override void OnDragEnter(DragEnterEvent evt)
        {
            base.OnDragEnter(evt);
            m_CurrentDropHighlightClass = CurrentDropAccepted ? dropHighlightAcceptedClass : dropHighlightDeniedClass;
            AddToClassList(m_CurrentDropHighlightClass);
        }

        /// <inheritdoc />
        public override void OnDragPerform(DragPerformEvent evt)
        {
            base.OnDragPerform(evt);

            if (GraphView == null)
                return;

            var selectable = GraphView.GetSelection().Single(); // we already check earlier that we only have one

            if (selectable is IVariableDeclarationModel variable)
            {
                CommandDispatcher.Dispatch(new CreateVariableNodesCommand(variable, evt.mousePosition, connectAfterCreation: PortModel, autoAlign: true));
            }
            else
            {
                var portToConnect = GetPortToConnect(selectable);
                Assert.IsNotNull(portToConnect);

                CommandDispatcher.Dispatch(new CreateEdgeCommand(PortModel, portToConnect, portAlignment: PortDirection.Input));
            }
        }

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            PartList.AppendPart(PortConnectorWithIconPart.Create(connectorPartName, Model, this, ussClassName));
            PartList.AppendPart(PortConstantEditorPart.Create(constantEditorPartName, Model, this, ussClassName));
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            EdgeConnector = new EdgeConnector(CommandDispatcher, GraphView, new EdgeConnectorListener());
            EdgeConnector.SetDropOutsideDelegate(OnDropOutsideCallback);

            AddToClassList(ussClassName);
            this.AddStylesheet("Port.uss");
        }

        /// <inheritdoc/>
        public override void AddModelDependencies()
        {
            if (PortModel.IsConnected())
            {
                foreach (var edgeModel in PortModel.GetConnectedEdges())
                {
                    AddDependencyToEdgeModel(edgeModel);
                }
            }
        }

        /// <summary>
        /// Add <paramref name="edgeModel"/> as a model dependency to this element.
        /// </summary>
        /// <param name="edgeModel">The model to add as a dependency.</param>
        public void AddDependencyToEdgeModel(IEdgeModel edgeModel)
        {
            // When edge is created/deleted, port connector needs to be updated (filled/unfilled).
            Dependencies.AddModelDependency(edgeModel);
        }

        protected void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(m_PortColorProperty, out var portColorValue))
                PortColor = portColorValue;

            if (PartList.GetPart(connectorPartName) is PortConnectorWithIconPart portConnector)
            {
                portConnector.UpdateFromModel();
            }
        }

        protected static string GetClassNameSuffixForType(PortType t)
        {
            return t.ToString().ToLower();
        }

        /// <inheritdoc />
        protected override void UpdateElementFromModel()
        {
            var hidden = PortModel?.Options.HasFlag(PortModelOptions.Hidden) ?? false;
            EnableInClassList(hiddenModifierUssClassName, hidden);

            EnableInClassList(connectedModifierUssClassName, PortModel.IsConnected());
            EnableInClassList(notConnectedModifierUssClassName, !PortModel.IsConnected());

            EnableInClassList(inputModifierUssClassName, PortModel.Direction == PortDirection.Input);
            EnableInClassList(outputModifierUssClassName, PortModel.Direction == PortDirection.Output);

            EnableInClassList(verticalModifierUssClassName, PortModel.Orientation == PortOrientation.Vertical);

            this.PrefixEnableInClassList(portDataTypeClassNamePrefix, GetClassNameSuffixForDataType(PortModel.PortDataType));

            this.PrefixEnableInClassList(portTypeModifierClassNamePrefix, GetClassNameSuffixForType(PortModel.PortType));

            tooltip = PortModel.Orientation == PortOrientation.Horizontal ? PortModel.ToolTip :
                string.IsNullOrEmpty(PortModel.ToolTip) ? PortModel.UniqueName :
                PortModel.UniqueName + "\n" + PortModel.ToolTip;
        }

        protected static string GetClassNameSuffixForDataType(Type thisPortType)
        {
            if (thisPortType == null)
                return String.Empty;

            if (thisPortType.IsSubclassOf(typeof(Component)))
                return "component";
            if (thisPortType.IsSubclassOf(typeof(GameObject)))
                return "game-object";
            if (thisPortType.IsSubclassOf(typeof(Rigidbody)) || thisPortType.IsSubclassOf(typeof(Rigidbody2D)))
                return "rigidbody";
            if (thisPortType.IsSubclassOf(typeof(Transform)))
                return "transform";
            if (thisPortType.IsSubclassOf(typeof(Texture)) || thisPortType.IsSubclassOf(typeof(Texture2D)))
                return "texture2d";
            if (thisPortType.IsSubclassOf(typeof(KeyCode)))
                return "key-code";
            if (thisPortType.IsSubclassOf(typeof(Material)))
                return "material";
            if (thisPortType == typeof(Object))
                return "object";
            return thisPortType.Name.ToKebabCase();
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

        protected void OnDropOutsideCallback(CommandDispatcher commandDispatcher, IEnumerable<Edge> edges, IEnumerable<IPortModel> ports, Vector2 pos)
        {
            if (commandDispatcher.State?.WindowState.GraphModel?.Stencil == null)
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

            ((Stencil)commandDispatcher.State.WindowState.GraphModel.Stencil)?.CreateNodesFromPort(commandDispatcher,
                existingPortModels, localPos, pos, edgesToDelete);
        }

        protected IPortModel GetPortToConnect(IGraphElementModel modelToDrop)
        {
            switch (modelToDrop)
            {
                case ISingleOutputPortNodeModel singleOutputPortNode when PortModel.Direction == PortDirection.Input:
                    return singleOutputPortNode.OutputPort;
                case ISingleInputPortNodeModel singleInputPortModelNode when PortModel.Direction == PortDirection.Output:
                    return singleInputPortModelNode.InputPort;
                default:
                    return null;
            }
        }

        protected bool HasModelToDrop(IGraphElementModel selectable)
        {
            var portToConnect = GetPortToConnect(selectable);
            return portToConnect != null && !ReferenceEquals(PortModel.NodeModel, portToConnect.NodeModel);
        }
    }
}
