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
    public class Port : VisualElementBridge, IGraphElement, IBadgeContainer, IDropTarget
    {
        public static readonly string k_UssClassName = "ge-port";
        public static readonly string k_WillConnectModifierUssClassName = k_UssClassName.WithUssModifier("will-connect");
        public static readonly string k_ConnectedModifierUssClassName = k_UssClassName.WithUssModifier("connected");
        public static readonly string k_NotConnectedModifierUssClassName = k_UssClassName.WithUssModifier("not-connected");
        public static readonly string k_InputModifierUssClassName = k_UssClassName.WithUssModifier("direction-input");
        public static readonly string k_OutputModifierUssClassName = k_UssClassName.WithUssModifier("direction-output");
        public static readonly string k_DropHighlightClass = k_UssClassName.WithUssModifier("drop-highlighted");
        public static readonly string k_PortDataTypeClassNamePrefix = k_UssClassName.WithUssModifier("data-type-");
        public static readonly string k_PortTypeModifierClassNamePrefix = k_UssClassName.WithUssModifier("type-");

        public static readonly string k_ConnectorPartName = "connector-container";
        public static readonly string k_ConstantEditorPartName = "constant-editor";

        Node m_Node;

        CustomStyleProperty<Color> m_PortColorProperty = new CustomStyleProperty<Color>("--port-color");

        protected ContextualMenuManipulator m_ContextualMenuManipulator;

        public GraphElementPartList PartList { get; private set; }

        public GraphView GraphView { get; private set; }

        public IPortModel PortModel { get; private set; }

        public IGraphElementModel Model => PortModel;

        public Store Store { get; private set; }

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
                foreach (var edgeModel in PortModel.GetConnectedEdges())
                {
                    var edge = edgeModel.GetUI<Edge>(GraphView);
                    edge?.UpdateFromModel();
                }
            }
        }

        public Orientation Orientation { get; set; }

        public Color PortColor { get; private set; }

        public IconBadge ErrorBadge { get; set; }

        public ValueBadge ValueBadge { get; set; }

        public Port()
        {
            m_ContextualMenuManipulator = new ContextualMenuManipulator(BuildContextualMenu);
            this.AddManipulator(m_ContextualMenuManipulator);

            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        public void SetupBuildAndUpdate(IGraphElementModel model, Store store, GraphView graphView)
        {
            Setup(model, store, graphView);
            BuildUI();
            UpdateFromModel();
        }

        public void Setup(IGraphElementModel portModel, Store store, GraphView graphView)
        {
            PortModel = portModel as IPortModel;
            Store = store;
            GraphView = graphView;

            PartList = new GraphElementPartList();
            BuildPartList();
        }

        protected virtual void BuildPartList()
        {
            if (!PortModel?.Options.HasFlag(PortModelOptions.Hidden) ?? true)
            {
                PartList.AppendPart(PortConnectorWithIconPart.Create(k_ConnectorPartName, Model, this, k_UssClassName));
                PartList.AppendPart(PortConstantEditorPart.Create(k_ConstantEditorPartName, Model, this, k_UssClassName, Store?.GetState()?.EditorDataModel));
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
        }

        protected virtual void PostBuildUI()
        {
            var connectorElement = this.Q(PortConnectorPart.k_ConnectorUssName) ?? this.Q(k_ConnectorPartName) ?? this;
            EdgeConnector = new EdgeConnector(Store, GraphView, new EdgeConnectorListener());
            EdgeConnector.SetDropOutsideDelegate(OnDropOutsideCallback);
            connectorElement.AddManipulator(EdgeConnector);

            AddToClassList(k_UssClassName);
            this.AddStylesheet("Port.uss");
        }

        public void UpdateFromModel()
        {
            if (!PortModel?.Options.HasFlag(PortModelOptions.Hidden) ?? true)
            {
                UpdateSelfFromModel();

                foreach (var component in PartList)
                {
                    component.UpdateFromModel();
                }
            }
        }

        static string GetClassNameModifierForType(PortType t)
        {
            return k_PortTypeModifierClassNamePrefix + t.ToString().ToLower();
        }

        protected virtual void UpdateSelfFromModel()
        {
            EnableInClassList(k_ConnectedModifierUssClassName, PortModel.IsConnected());
            EnableInClassList(k_NotConnectedModifierUssClassName, !PortModel.IsConnected());

            EnableInClassList(k_InputModifierUssClassName, PortModel.Direction == Direction.Input);
            EnableInClassList(k_OutputModifierUssClassName, PortModel.Direction == Direction.Output);

            this.PrefixRemoveFromClassList(k_PortDataTypeClassNamePrefix);
            AddToClassList(GetClassNameForDataType(PortModel.PortDataType));

            this.PrefixRemoveFromClassList(k_PortTypeModifierClassNamePrefix);
            AddToClassList(GetClassNameModifierForType(PortModel.PortType));

            tooltip = PortModel.ToolTip;
        }

        protected virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt) {}

        void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            if (e.customStyle.TryGetValue(m_PortColorProperty, out var portColorValue))
                PortColor = portColorValue;

            if (PartList.GetPart(k_ConnectorPartName) is PortConnectorWithIconPart portConnector)
            {
                portConnector.UpdateFromModel();
            }
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
            var portConnector = PartList.GetPart(k_ConnectorPartName) as PortConnectorPart;
            var connector = portConnector?.Root.Q(PortConnectorPart.k_ConnectorUssName) ?? portConnector?.Root ?? this;
            return connector;
        }

        static void OnDropOutsideCallback(Store store, Edge edge, Vector2 pos)
        {
            if (store?.GetState()?.CurrentGraphModel?.Stencil == null)
                return;

            GraphView graphView = edge.GetFirstAncestorOfType<GraphView>();
            Vector2 localPos = graphView.contentViewContainer.WorldToLocal(pos);

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

            store.GetState().CurrentGraphModel.Stencil.CreateNodesFromPort(store, existingPortModel, localPos, pos, edgesToDelete);
        }

        public virtual bool CanAcceptDrop(List<ISelectableGraphElement> dragSelection)
        {
            return dragSelection.Count == 1 && PortModel.PortType != PortType.Execution && IsTokenToDrop(dragSelection[0]);
        }

        bool IsTokenToDrop(ISelectableGraphElement selectable)
        {
            return selectable is TokenNode token
                && token.Model is IVariableNodeModel varModel
                && varModel.OutputPort.GetConnectedPorts().All(p => p != PortModel)
                && !ReferenceEquals(PortModel.NodeModel, token.Model);
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

            if (IsTokenToDrop(selectionList[0]))
            {
                if (selectionList[0] is TokenNode token)
                {
                    token.Model.SetCapability(Capabilities.Movable, true);
                    Store.Dispatch(new CreateEdgeAction(PortModel, ((IVariableNodeModel)token.Model).OutputPort, edgesToDelete, CreateEdgeAction.PortAlignmentType.Input));
                }
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
            AddToClassList(k_DropHighlightClass);
            var dragSelection = selection.ToList();
            if (dragSelection.Count == 1 && dragSelection[0] is TokenNode token)
                token.Model.SetCapability(Capabilities.Movable, false);
            return true;
        }

        public virtual bool DragLeave(DragLeaveEvent evt, IEnumerable<ISelectableGraphElement> selection, IDropTarget leftTarget, ISelection dragSource)
        {
            RemoveFromClassList(k_DropHighlightClass);
            var dragSelection = selection.ToList();
            if (dragSelection.Count == 1 && dragSelection[0] is TokenNode token)
                token.Model.SetCapability(Capabilities.Movable, true);
            return true;
        }

        public virtual bool DragExited()
        {
            RemoveFromClassList(k_DropHighlightClass);
            return false;
        }
    }
}
