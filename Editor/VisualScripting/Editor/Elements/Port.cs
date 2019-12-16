using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Editor.ConstantEditor;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    public class Port : Experimental.GraphView.Port, IDropTarget
    {
        const string k_DropHighlightClass = "drop-highlighted";
        const string k_DropHighlightDeniedClass = "denied";

        IPortModel Model => (IPortModel)userData;

        VisualElement m_InputEditor; // if this port allows editing an input, holds the element editing it

        Store m_Store;
        VisualElement PortIcon { get; set; }

        VseGraphView m_VseGraphView;

        Rect m_BoxRect = Rect.zero;

        VseGraphView GraphView => m_VseGraphView ?? (m_VseGraphView = GetFirstAncestorOfType<VseGraphView>());

        // TODO: Weird that ContainsPoint does not work out of the box (with the default implementation)
        public override bool ContainsPoint(Vector2 localPoint)
        {
            if (m_BoxRect == Rect.zero)
            {
                Rect lRect = m_ConnectorBox.layout;

                if (direction == Direction.Input)
                {
                    m_BoxRect = new Rect(-lRect.xMin, -lRect.yMin, lRect.width + lRect.xMin, layout.height);
                    m_BoxRect.width += m_ConnectorText.layout.xMin - lRect.xMax;
                }
                else
                {
                    m_BoxRect = new Rect(0, -lRect.yMin, layout.width - lRect.xMin, layout.height);
                    float leftSpace = lRect.xMin - m_ConnectorText.layout.xMax;

                    m_BoxRect.xMin -= leftSpace;
                    m_BoxRect.width += leftSpace;
                }

                float hitBoxExtraPadding = m_BoxRect.height;
                if (orientation == Orientation.Horizontal)
                {
                    // Add extra padding for mouse check to the left of input port or right of output port.
                    if (direction == Direction.Input)
                    {
                        // Move bounds to the left by hitBoxExtraPadding and increase width
                        // by hitBoxExtraPadding.
                        m_BoxRect.x -= hitBoxExtraPadding;
                        m_BoxRect.width += hitBoxExtraPadding * ((userData as PortModel)?.NodeModel is IStackModel ? 2 : 1);
                    }
                    else if (direction == Direction.Output)
                    {
                        // Just add hitBoxExtraPadding to the width.
                        m_BoxRect.width += hitBoxExtraPadding;
                    }
                }
                else
                {
                    // Add extra padding for mouse check to the top of input port or bottom of output port.
                    if (direction == Direction.Input)
                    {
                        // Move bounds to the top by hitBoxExtraPadding and increase height
                        // by hitBoxExtraPadding.
                        m_BoxRect.y -= hitBoxExtraPadding;
                        m_BoxRect.height += hitBoxExtraPadding;
                    }
                    else if (direction == Direction.Output)
                    {
                        // Just add hitBoxExtraPadding to the height.
                        m_BoxRect.height += hitBoxExtraPadding;
                    }
                }
            }

            return m_BoxRect.Contains(this.ChangeCoordinatesTo(m_ConnectorBox, localPoint));
        }

        static List<IEdgeModel> GetDropEdgeModelsToDelete(Edge edge)
        {
            List<IEdgeModel> edgeModelsToDelete = new List<IEdgeModel>();

            if (edge.input?.capacity == Capacity.Single)
            {
                foreach (var e in edge.input?.connections)
                {
                    var edgeToDelete = (Edge)e;
                    if (edgeToDelete != edge)
                        edgeModelsToDelete.Add(edgeToDelete.model);
                }
            }

            if (edge.output?.capacity == Capacity.Single)
            {
                foreach (var e in edge.output?.connections)
                {
                    var edgeToDelete = (Edge)e;
                    if (edgeToDelete != edge)
                        edgeModelsToDelete.Add(edgeToDelete.model);
                }
            }

            return edgeModelsToDelete;
        }

        public static Port Create(IPortModel model, Store store, Orientation orientation, VisualElement existingIcon = null)
        {
            var stencil = model.GraphModel.Stencil;

            var ele = new Port(orientation, model.Direction, model.Capacity, model.DataType.Resolve(stencil))
            {
                userData = model,
                m_Store = store,
                portName = model.Name,
            };

            var connector = new EdgeConnector<Edge>(new VseEdgeConnectorListener(
                (edge, pos) => OnDropOutsideCallback(store, pos, (Edge)edge),
                edge => OnDropCallback(store, edge)));

            ele.m_EdgeConnector = connector;

            ele.UpdateTooltip(model);

            ele.EnableInClassList("data", model.PortType == PortType.Data);
            ele.EnableInClassList("instance", model.PortType == PortType.Instance);
            ele.EnableInClassList("execution", model.PortType == PortType.Execution);
            ele.EnableInClassList("loop", model.PortType == PortType.Loop);
            ele.EnableInClassList("event", model.PortType == PortType.Event);

            ele.AddManipulator(ele.m_EdgeConnector);

            if (model.PortType == PortType.Data || model.PortType == PortType.Instance)
            {
                if (existingIcon == null)
                {
                    existingIcon = new Image { name = "portIcon" };
                    ele.Insert(1, existingIcon); // on output, flex-direction is reversed, so the icon will effectively be BEFORE the connector
                }
                existingIcon.AddToClassList(((PortModel)model).IconTypeString);
            }

            ele.PortIcon = existingIcon;

            ele.MarkDirtyRepaint();

            return ele;
        }

        IConstantNodeModel GetModelToWatch()
        {
            // only watch a model on input ports
            return direction == Direction.Input ? (Model as PortModel)?.EmbeddedValue : null;
        }

        public static Port CreateInputPort(Store store, IPortModel inputPortModel, VisualElement instanceContainer, VisualElement dataContainer, VisualElement existingIcon = null, Orientation orientation = Orientation.Horizontal)
        {
            Assert.IsTrue(inputPortModel.Direction == Direction.Input, "Expected input port model");

            var port = Create(inputPortModel, store, orientation, existingIcon);

            if (inputPortModel.PortType == PortType.Instance)
            {
                port.AddToClassList("instance");
                instanceContainer.Insert(0, port);
            }
            else
            {
                VisualElement innerContainer = new VisualElement {name = "innerContainer"};
                innerContainer.style.flexDirection = FlexDirection.Row;
                innerContainer.Add(port);
                dataContainer.Add(innerContainer);

                IConstantNodeModel modelToShow = port.GetModelToWatch();

                if (modelToShow != null)
                {
                    VisualElement editor = port.CreateEditorForNodeModel(modelToShow, _ => store.Dispatch(new RefreshUIAction(UpdateFlags.RequestCompilation)));
                    if (editor != null)
                    {
                        innerContainer.Add(editor);
                        port.m_InputEditor = editor;
                        port.m_InputEditor.SetEnabled(!port.Model.Connected || port.Model.ConnectionPortModels.All(p => p.NodeModel.State == ModelState.Disabled));
                    }
                }
            }

            return port;
        }

        static void OnDropCallback(Store store, Experimental.GraphView.Edge edge)
        {
            List<IEdgeModel> edgeModelsToDelete = GetDropEdgeModelsToDelete((Edge)edge);

            if (((Port)edge.input).IsConnectedTo((Port)edge.output))
                return;

            // when grabbing an existing edge's end, the edgeModel should be deleted
            if (((Edge)edge).GraphElementModel != null)
                edgeModelsToDelete.Add((IEdgeModel)((Edge)edge).GraphElementModel);

            store.Dispatch(new CreateEdgeAction(
                ((Port)edge.input).userData as IPortModel,
                ((Port)edge.output).userData as IPortModel,
                edgeModelsToDelete
            ));
        }

        static void OnDropOutsideCallback(Store store, Vector2 pos, Edge edge)
        {
            VseGraphView graphView = edge.GetFirstAncestorOfType<VseGraphView>();
            Vector2 worldPos = pos;
            pos = graphView.contentViewContainer.WorldToLocal(pos);

            List<IEdgeModel> edgesToDelete = GetDropEdgeModelsToDelete(edge);

            // when grabbing an existing edge's end, the edgeModel should be deleted
            if ((edge).GraphElementModel != null)
                edgesToDelete.Add((IEdgeModel)edge.GraphElementModel);

            IStackModel targetStackModel = null;
            int targetIndex = -1;
            StackNode stackNode = graphView.lastHoveredVisualElement as StackNode ??
                graphView.lastHoveredVisualElement.GetFirstOfType<StackNode>();

            if (stackNode != null)
            {
                targetStackModel = stackNode.stackModel;
                targetIndex = stackNode.GetInsertionIndex(worldPos);
            }

            IPortModel existingPortModel;
            // warning: when dragging the end of an existing edge, both ports are non null.
            if (edge.input != null && edge.output != null)
            {
                float distanceToOutput = Vector2.Distance(edge.edgeControl.from, pos);
                float distanceToInput = Vector2.Distance(edge.edgeControl.to, pos);
                // note: if the user was able to stack perfectly both ports, we'd be in trouble
                if (distanceToOutput < distanceToInput)
                    existingPortModel = (IPortModel)edge.input.userData;
                else
                    existingPortModel = (IPortModel)edge.output.userData;
            }
            else
            {
                Port existingPort = (Port)(edge.input ?? edge.output);
                existingPortModel = existingPort.userData as IPortModel;
            }

            store.GetState().CurrentGraphModel?.Stencil.CreateNodesFromPort(store, existingPortModel, pos, edgesToDelete, targetStackModel, targetIndex);
        }

        // TODO type might be void if a variable setter has no variable reference; use unknown instead
        Port(Orientation portOrientation, Direction portDirection, Capacity portCapacity, Type type)
            : base(portOrientation, portDirection, portCapacity, type == typeof(void) || type.ContainsGenericParameters ? typeof(Unknown) : type)
        {
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "Port.uss"));

            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        public override void Connect(Experimental.GraphView.Edge edge)
        {
            base.Connect(edge);
            EnableInClassList("disconnected", !connected);
        }

        public override void Disconnect(Experimental.GraphView.Edge edge)
        {
            base.Disconnect(edge);
            EnableInClassList("disconnected", !connected);
        }

        public override void DisconnectAll()
        {
            base.DisconnectAll();
            EnableInClassList("disconnected", !connected);
        }

        bool IsConnectedTo(Port other)
        {
            // If this becomes a bottleneck we can revert to use 2 foreach() and break
            // when we have a successful connection test
            return (from edge in connections
                from otherEdge in other.connections
                where
                otherEdge.input == edge.input && otherEdge.output == edge.output
                select edge).Any();
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            // TODO: Not the proper way; should use style property --unity-image-tint-color instead;
            //       however, since portColor is variable, we cannot use until there is proper support for variables
            if (PortIcon is Image portIconImage)
                portIconImage.tintColor = portColor;
        }

        public bool CanAcceptDrop(List<ISelectable> dragSelection)
        {
            return dragSelection.Count == 1 &&
                ((userData as IPortModel)?.PortType != PortType.Execution &&
                    (dragSelection[0] is IVisualScriptingField
                        || dragSelection[0] is TokenDeclaration
                        || IsTokenToDrop(dragSelection[0])));
        }

        bool IsTokenToDrop(ISelectable selectable)
        {
            return selectable is Token token
                && token.GraphElementModel is IVariableModel varModel
                && !varModel.OutputPort.ConnectionPortModels.Any(p => p == Model);
        }

        public bool DragUpdated(DragUpdatedEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget, ISelection dragSource)
        {
            return true;
        }

        public bool DragPerform(DragPerformEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget, ISelection dragSource)
        {
            if (GraphView == null)
                return false;

            List<ISelectable> selectionList = selection.ToList();
            List<GraphElement> dropElements = selectionList.OfType<GraphElement>().ToList();

            Assert.IsTrue(dropElements.Count == 1);
            GraphElement elementToDrop = dropElements.First();

            var edgesToDelete = capacity == Capacity.Multi ? new List<IEdgeModel>() : connections.Select(c => ((Edge)c).model).ToList();

            if (IsTokenToDrop(selectionList[0]))
            {
                Token token = ((Token)selectionList[0]);
                token.NeedStoreDispatch = true;
                m_Store.Dispatch(new CreateEdgeAction(Model, ((IVariableModel)token.GraphElementModel).OutputPort, edgesToDelete, CreateEdgeAction.PortAlignmentType.Input));
                return true;
            }

            List<Tuple<IVariableDeclarationModel, Vector2>> variablesToCreate = DragAndDropHelper.ExtractVariablesFromDroppedElements(dropElements, GraphView, evt.mousePosition);

            PortType type = ((IPortModel)userData).PortType;
            if (type != PortType.Data && type != PortType.Instance) // do not connect loop/exec ports to variables
            {
                return GraphView.DragPerform(evt, selectionList, dropTarget, dragSource);
            }

            IVariableDeclarationModel varModelToCreate = variablesToCreate.Single().Item1;

            m_Store.Dispatch(new CreateVariableNodesAction(varModelToCreate, evt.mousePosition, edgesToDelete, (IPortModel)userData, autoAlign: true));

            GraphView.ClearPlaceholdersAfterDrag();

            return true;
        }

        public bool DragEnter(DragEnterEvent evt, IEnumerable<ISelectable> selection, IDropTarget enteredTarget, ISelection dragSource)
        {
            AddToClassList(k_DropHighlightClass);
            var dragSelection = selection.ToList();
            EnableInClassList(k_DropHighlightDeniedClass, !CanAcceptDrop(dragSelection));
            if (dragSelection.Count == 1 && dragSelection[0] is Token token)
                token.NeedStoreDispatch = false;
            return true;
        }

        public bool DragLeave(DragLeaveEvent evt, IEnumerable<ISelectable> selection, IDropTarget leftTarget, ISelection dragSource)
        {
            RemoveFromClassList(k_DropHighlightClass);
            var dragSelection = selection.ToList();
            if (dragSelection.Count == 1 && dragSelection[0] is Token token)
                token.NeedStoreDispatch = true;
            return true;
        }

        public bool DragExited()
        {
            RemoveFromClassList(k_DropHighlightClass);
            GraphView?.ClearPlaceholdersAfterDrag();
            return false;
        }

        // TODO: once we have partial ui rebuild, fix that
        [PublicAPI]
        public void UpdateTooltip(IPortModel portModel)
        {
            tooltip = m_Store.GetState().CurrentGraphModel?.Stencil.GetTooltipForPortModel(portModel);
        }
    }
}
