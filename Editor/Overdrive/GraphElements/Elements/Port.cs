using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEditor.GraphToolsFoundation.Overdrive.InternalModels;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class Port : VisualElementBridge, IGraphElement, IDropTarget
    {
        public static readonly string ussClassName = "ge-port";
        public static readonly string highlightedModifierUssClassName = ussClassName.WithUssModifier("highlighted");
        public static readonly string willConnectModifierUssClassName = ussClassName.WithUssModifier("will-connect");
        public static readonly string connectedModifierUssClassName = ussClassName.WithUssModifier("connected");
        public static readonly string notConnectedModifierUssClassName = ussClassName.WithUssModifier("not-connected");
        public static readonly string inputModifierUssClassName = ussClassName.WithUssModifier("direction-input");
        public static readonly string outputModifierUssClassName = ussClassName.WithUssModifier("direction-output");
        public static readonly string dropHighlightClass = ussClassName.WithUssModifier("drop-highlighted");
        public static readonly string portDataTypeClassNamePrefix = ussClassName.WithUssModifier("data-type-");
        public static readonly string portTypeModifierClassNamePrefix = ussClassName.WithUssModifier("type-");

        public static readonly string connectorPartName = "connector-container";
        public static readonly string constantEditorPartName = "constant-editor";

        Node m_Node;

        CustomStyleProperty<Color> m_PortColorProperty = new CustomStyleProperty<Color>("--port-color");

        ContextualMenuManipulator m_ContextualMenuManipulator;

        EdgeConnector m_EdgeConnector;

        protected ContextualMenuManipulator ContextualMenuManipulator
        {
            get => m_ContextualMenuManipulator;
            set => this.ReplaceManipulator(ref m_ContextualMenuManipulator, value);
        }

        public GraphElementPartList PartList { get; private set; }

        public GraphView GraphView { get; private set; }

        public string Context { get; private set; }

        public IPortModel PortModel { get; private set; }

        public IGraphElementModel Model => PortModel;

        public Store Store { get; private set; }

        protected UIDependencies Dependencies { get; }

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

        public Port()
        {
            ContextualMenuManipulator = new ContextualMenuManipulator(BuildContextualMenu);

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);

            Dependencies = new UIDependencies(this);
        }

        public void SetupBuildAndUpdate(IGraphElementModel model, Store store, GraphView graphView, string context)
        {
            Setup(model, store, graphView, context);
            BuildUI();
            UpdateFromModel();
        }

        public void Setup(IGraphElementModel portModel, Store store, GraphView graphView, string context)
        {
            PortModel = portModel as IPortModel;
            Store = store;
            GraphView = graphView;
            Context = context;

            PartList = new GraphElementPartList();
            BuildPartList();
        }

        public void AddToGraphView(GraphView graphView)
        {
            GraphView = graphView;
            UIForModel.AddOrReplaceGraphElement(this);

            foreach (var component in PartList)
            {
                component.OwnerAddedToGraphView();
            }
        }

        public void RemoveFromGraphView()
        {
            foreach (var component in PartList)
            {
                component.OwnerRemovedFromGraphView();
            }

            Dependencies.ClearDependencyLists();
            UIForModel.RemoveGraphElement(this);
            GraphView = null;
        }

        protected virtual void BuildPartList()
        {
            if (!PortModel?.Options.HasFlag(PortModelOptions.Hidden) ?? true)
            {
                PartList.AppendPart(PortConnectorWithIconPart.Create(connectorPartName, Model, this, ussClassName));
                PartList.AppendPart(PortConstantEditorPart.Create(constantEditorPartName, Model, this, ussClassName));
            }
        }

        public void BuildUI()
        {
            if (!PortModel?.Options.HasFlag(PortModelOptions.Hidden) ?? true)
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
        }

        protected virtual void BuildSelfUI()
        {
            Orientation = PortModel?.Orientation ?? Orientation.Horizontal;
        }

        protected virtual void PostBuildUI()
        {
            EdgeConnector = new EdgeConnector(Store, GraphView, new EdgeConnectorListener());
            EdgeConnector.SetDropOutsideDelegate(OnDropOutsideCallback);

            AddToClassList(ussClassName);
            this.AddStylesheet("Port.uss");
        }

        public void UpdateFromModel()
        {
            if (Store?.State?.Preferences.GetBool(BoolPref.LogUIUpdate) ?? false)
            {
                Debug.LogWarning($"Rebuilding {this} ({Store.State.LastDispatchedActionName})");
            }

            if (!PortModel?.Options.HasFlag(PortModelOptions.Hidden) ?? true)
            {
                UpdateSelfFromModel();

                foreach (var component in PartList)
                {
                    component.UpdateFromModel();
                }
            }

            Dependencies.UpdateDependencyLists();
        }

        public virtual void AddForwardDependencies()
        {
        }

        public virtual void AddBackwardDependencies()
        {
        }

        public virtual void AddModelDependencies()
        {
            if (PortModel.IsConnected())
            {
                foreach (var edgeModel in PortModel.GetConnectedEdges())
                {
                    Dependencies.AddModelDependency(edgeModel);
                }
            }
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            Dependencies.OnGeometryChanged(evt);
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(m_PortColorProperty, out var portColorValue))
                PortColor = portColorValue;

            if (PartList.GetPart(connectorPartName) is PortConnectorWithIconPart portConnector)
            {
                portConnector.UpdateFromModel();
            }

            Dependencies.OnCustomStyleResolved(evt);
        }

        void OnDetachedFromPanel(DetachFromPanelEvent evt)
        {
            Dependencies.OnDetachedFromPanel(evt);
        }

        static string GetClassNameModifierForType(PortType t)
        {
            return portTypeModifierClassNamePrefix + t.ToString().ToLower();
        }

        protected virtual void UpdateSelfFromModel()
        {
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

        protected virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt) {}

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

        void OnDropOutsideCallback(Store store, Edge edge, Vector2 pos)
        {
            if (store.State?.GraphModel?.Stencil == null)
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

            store.State.GraphModel.Stencil.CreateNodesFromPort(store, existingPortModel, localPos, pos, edgesToDelete);
        }

        public virtual bool CanAcceptDrop(List<ISelectableGraphElement> dragSelection)
        {
            return dragSelection.Count == 1 && PortModel.PortType != PortType.Execution && HasModelToDrop(dragSelection[0]);
        }

        IPortModel GetPortToConnect(ISelectableGraphElement selectable)
        {
            if (selectable is GraphElement graphElement)
            {
                var modelToDrop = graphElement.Model;
                if (modelToDrop is ISingleOutputPortNode singleOutputPortNode && PortModel.Direction == Direction.Input)
                    return singleOutputPortNode.OutputPort;
                if (modelToDrop is ISingleInputPortNode singleInputPortModelNode && PortModel.Direction == Direction.Output)
                    return singleInputPortModelNode.InputPort;
            }

            return null;
        }

        bool HasModelToDrop(ISelectableGraphElement selectable)
        {
            var portToConnect = GetPortToConnect(selectable);
            return portToConnect != null && !ReferenceEquals(PortModel.NodeModel, portToConnect.NodeModel);
        }

        public virtual bool DragUpdated(DragUpdatedEvent evt, IEnumerable<ISelectableGraphElement> selection, IDropTarget dropTarget, ISelection dragSource)
        {
            return true;
        }

        public virtual bool DragPerform(DragPerformEvent evt, IEnumerable<ISelectableGraphElement> selection, IDropTarget dropTarget, ISelection dragSource)
        {
            if (GraphView == null)
                return false;

            List<ISelectableGraphElement> selectionList = selection.ToList();
            List<GraphElement> dropElements = selectionList.OfType<GraphElement>().ToList();

            Assert.IsTrue(dropElements.Count == 1);

            var edgesToDelete = PortModel.Capacity == PortCapacity.Multi ? new List<IEdgeModel>() : PortModel.GetConnectedEdges().ToList();
            var portToConnect = GetPortToConnect(selectionList[0]);

            if (portToConnect != null)
            {
                Store.Dispatch(new CreateEdgeAction(PortModel, portToConnect, edgesToDelete, Direction.Input));
                return true;
            }

            var variablesToCreate = GraphView.ExtractVariablesFromDroppedElements(dropElements, evt.mousePosition);

            PortType type = PortModel.PortType;
            if (type != PortType.Data) // do not connect loop/exec ports to variables
            {
                return (GraphView as GtfoGraphView)?.DragPerform(evt, selectionList, dropTarget, dragSource) ?? true;
            }

            IVariableDeclarationModel varModelToCreate = variablesToCreate.Single().Item1;

            Store.Dispatch(new CreateVariableNodesAction(varModelToCreate, evt.mousePosition, edgesToDelete, PortModel, autoAlign: true));

            return true;
        }

        public virtual bool DragEnter(DragEnterEvent evt, IEnumerable<ISelectableGraphElement> selection, IDropTarget enteredTarget, ISelection dragSource)
        {
            AddToClassList(dropHighlightClass);
            return true;
        }

        public virtual bool DragLeave(DragLeaveEvent evt, IEnumerable<ISelectableGraphElement> selection, IDropTarget leftTarget, ISelection dragSource)
        {
            RemoveFromClassList(dropHighlightClass);
            return true;
        }

        public virtual bool DragExited()
        {
            RemoveFromClassList(dropHighlightClass);
            return false;
        }
    }
}
