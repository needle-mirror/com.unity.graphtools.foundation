using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.InternalModels;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// UI for a IPortModel
    /// Allows connection of Edges
    /// Handles dropping of elements on top of them to create an edge
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
                var connectorElement = this.Q(PortConnectorPart.connectorUssName) ?? this.Q(connectorPartName) ?? this;
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

        public Orientation Orientation { get; private set; }

        public Color PortColor { get; private set; }

        private string m_CurrentDropHighlightClass = dropHighlightAcceptedClass;

        public Port()
        {
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        public override bool CanAcceptSelectionDrop(IReadOnlyList<ISelectableGraphElement> dragSelection)
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

            var selectable = GraphView.Selection.Single(); // we already check earlier that we only have one

            if (selectable is BlackboardField field)
                OnDropBlackboardField(field, evt.mousePosition);
            else
                OnDropElement(selectable);
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

        protected override void UpdateElementFromModel()
        {
            Orientation = PortModel?.Orientation ?? Orientation.Horizontal;

            var hidden = PortModel.Options.HasFlag(PortModelOptions.Hidden);
            EnableInClassList(hiddenModifierUssClassName, hidden);

            EnableInClassList(connectedModifierUssClassName, PortModel.IsConnected());
            EnableInClassList(notConnectedModifierUssClassName, !PortModel.IsConnected());

            EnableInClassList(inputModifierUssClassName, PortModel.Direction == Direction.Input);
            EnableInClassList(outputModifierUssClassName, PortModel.Direction == Direction.Output);

            this.PrefixRemoveFromClassList(portDataTypeClassNamePrefix);
            AddToClassList(GetClassNameForDataType(PortModel.PortDataType));

            this.PrefixRemoveFromClassList(portTypeModifierClassNamePrefix);
            AddToClassList(GetClassNameModifierForType(PortModel.PortType));

            tooltip = PortModel.ToolTip;
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
            var connector = portConnector?.Root.Q(PortConnectorPart.connectorUssName) ?? portConnector?.Root ?? this;
            return connector;
        }

        void OnDropOutsideCallback(CommandDispatcher commandDispatcher, Edge edge, Vector2 pos)
        {
            if (commandDispatcher.GraphToolState?.GraphModel?.Stencil == null)
                return;

            Vector2 localPos = GraphView.contentViewContainer.WorldToLocal(pos);

            List<IEdgeModel> edgesToDelete = EdgeConnectorListener.GetDropEdgeModelsToDelete(edge.EdgeModel);

            // when grabbing an existing edge's end, the edgeModel should be deleted
            if (edge.EdgeModel != null && !(edge.EdgeModel is GhostEdgeModel))
                edgesToDelete.Add(edge.EdgeModel);

            IPortModel existingPortModel;
            // warning: when dragging the end of an existing edge, both ports are non null.
            if (edge.Input != null && edge.Output != null)
            {
                float distanceToOutput = Vector2.Distance(edge.From, pos);
                float distanceToInput = Vector2.Distance(edge.To, pos);
                // note: if the user was able to stack perfectly both ports, we'd be in trouble
                if (distanceToOutput < distanceToInput)
                    existingPortModel = edge.Input;
                else
                    existingPortModel = edge.Output;
            }
            else
            {
                existingPortModel = edge.Input ?? edge.Output;
            }

            commandDispatcher.GraphToolState.GraphModel.Stencil.CreateNodesFromPort(commandDispatcher,
                existingPortModel, localPos, pos, edgesToDelete);
        }

        IPortModel GetPortToConnect(ISelectableGraphElement selectable)
        {
            if (selectable is GraphElement graphElement)
            {
                var modelToDrop = graphElement.Model;
                if (modelToDrop is ISingleOutputPortNode singleOutputPortNode && PortModel.Direction == Direction.Input)
                    return singleOutputPortNode.OutputPort;
                if (modelToDrop is ISingleInputPortNode singleInputPortModelNode &&
                    PortModel.Direction == Direction.Output)
                    return singleInputPortModelNode.InputPort;
            }

            return null;
        }

        bool HasModelToDrop(ISelectableGraphElement selectable)
        {
            var portToConnect = GetPortToConnect(selectable);
            return portToConnect != null && !ReferenceEquals(PortModel.NodeModel, portToConnect.NodeModel);
        }

        protected virtual void OnDropElement(ISelectableGraphElement selectable)
        {
            var portToConnect = GetPortToConnect(selectable);
            Assert.IsNotNull(portToConnect);

            CommandDispatcher.Dispatch(new CreateEdgeCommand(PortModel, portToConnect, portAlignment: Direction.Input));
        }

        protected virtual void OnDropBlackboardField(BlackboardField blackboardField, Vector2 mousePosition)
        {
            var stencil = GraphView.CommandDispatcher.GraphToolState.AssetModel.GraphModel.Stencil;
            var variablesToCreate = stencil.ExtractVariableFromGraphElement(blackboardField);

            CommandDispatcher.Dispatch(new CreateVariableNodesCommand(variablesToCreate, mousePosition, connectAfterCreation: PortModel, autoAlign: true));
        }
    }
}
