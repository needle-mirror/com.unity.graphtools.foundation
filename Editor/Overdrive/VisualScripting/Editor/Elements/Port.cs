using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class Port : GraphElements.Port, IDropTarget
    {
        public static readonly string k_PortTypeModifierClassNamePrefix = "ge-port--type-";
        public static readonly string k_PortExecutionActiveModifer = k_UssClassName.WithUssModifier("execution-active");
        public static readonly string k_DropHighlightClass = k_UssClassName.WithUssModifier("drop-highlighted");
        public static readonly string k_ConstantEditorPartName = "constant-editor";

        VseGraphView VseGraphView => GraphView as VseGraphView;

        /// <summary>
        /// Used to highlight the port when it is triggered during tracing
        /// </summary>
        public bool ExecutionPortActive
        {
            set => EnableInClassList(k_PortExecutionActiveModifer, value);
        }

        public Port()
        {
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            if (PartList.GetPart(k_ConnectorPartName) is PortConnectorWithIconPart portConnector)
            {
                portConnector.UpdateFromModel();
            }
        }

        protected override void BuildPartList()
        {
            if (!(PortModel as PortModel)?.Options.HasFlag(PortModelOptions.Hidden) ?? true)
            {
                base.BuildPartList();

                PartList.ReplacePart(k_ConnectorPartName, PortConnectorWithIconPart.Create(k_ConnectorPartName, Model, this, k_UssClassName));
                PartList.AppendPart(PortConstantEditorPart.Create(k_ConstantEditorPartName, Model, this, k_UssClassName, Store.GetState()?.EditorDataModel));
            }
        }

        protected override void BuildSelfUI()
        {
            if (!(PortModel as PortModel)?.Options.HasFlag(PortModelOptions.Hidden) ?? true)
            {
                base.BuildSelfUI();
            }
        }

        protected override void PostBuildUI()
        {
            if (!(PortModel as PortModel)?.Options.HasFlag(PortModelOptions.Hidden) ?? true)
            {
                base.PostBuildUI();

                EdgeConnector?.SetDropOutsideDelegate((s, edge, pos) => OnDropOutsideCallback(s, pos, edge));
                styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "Port.uss"));
            }
        }

        static void OnDropOutsideCallback(Store store, Vector2 pos, Edge edge)
        {
            VseGraphView graphView = edge.GetFirstAncestorOfType<VseGraphView>();
            Vector2 localPos = graphView.contentViewContainer.WorldToLocal(pos);

            List<IGTFEdgeModel> edgesToDelete = EdgeConnectorListener.GetDropEdgeModelsToDelete(edge.EdgeModel);

            // when grabbing an existing edge's end, the edgeModel should be deleted
            if (edge.EdgeModel != null && !(edge.EdgeModel is GhostEdgeModel))
                edgesToDelete.Add(edge.EdgeModel);

            IGTFPortModel existingPortModel;
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

            ((Store)store)?.GetState().CurrentGraphModel?.Stencil.CreateNodesFromPort((Store)store, existingPortModel, localPos, pos, edgesToDelete);
        }

        protected override void UpdateSelfFromModel()
        {
            base.UpdateSelfFromModel();

            this.PrefixRemoveFromClassList(k_PortTypeModifierClassNamePrefix);
            AddToClassList(GetClassNameModifierForType(PortModel.PortType));
        }

        static string GetClassNameModifierForType(PortType t)
        {
            return k_PortTypeModifierClassNamePrefix + t.ToString().ToLower();
        }

        public bool CanAcceptDrop(List<ISelectableGraphElement> dragSelection)
        {
            return dragSelection.Count == 1 &&
                (PortModel.PortType != PortType.Execution &&
                    (dragSelection[0] is IVisualScriptingField
                        || dragSelection[0] is TokenDeclaration
                        || IsTokenToDrop(dragSelection[0])));
        }

        bool IsTokenToDrop(ISelectableGraphElement selectable)
        {
            return selectable is Token token
                && token.Model is IGTFVariableNodeModel varModel
                && !varModel.OutputPort.GetConnectedPorts().Any(p => p == PortModel)
                && !ReferenceEquals(PortModel.NodeModel, token.Model);
        }

        public bool DragUpdated(DragUpdatedEvent evt, IEnumerable<ISelectableGraphElement> selection, IDropTarget dropTarget, ISelection dragSource)
        {
            return true;
        }

        public bool DragPerform(DragPerformEvent evt, IEnumerable<ISelectableGraphElement> selection, IDropTarget dropTarget, ISelection dragSource)
        {
            if (GraphView == null)
                return false;

            List<ISelectableGraphElement> selectionList = selection.ToList();
            List<GraphElement> dropElements = selectionList.OfType<GraphElement>().ToList();

            Assert.IsTrue(dropElements.Count == 1);

            var edgesVMToDelete = PortModel.Capacity == PortCapacity.Multi ? new List<IGTFEdgeModel>() : PortModel.GetConnectedEdges();
            var edgesToDelete = edgesVMToDelete;

            if (IsTokenToDrop(selectionList[0]))
            {
                Token token = ((Token)selectionList[0]);
                token.SetMovable(true);
                Store.Dispatch(new CreateEdgeAction(PortModel, ((IGTFVariableNodeModel)token.Model).OutputPort, edgesToDelete, CreateEdgeAction.PortAlignmentType.Input));
                return true;
            }

            var variablesToCreate = DragAndDropHelper.ExtractVariablesFromDroppedElements(dropElements, GraphView, evt.mousePosition);

            PortType type = PortModel.PortType;
            if (type != PortType.Data) // do not connect loop/exec ports to variables
            {
                return VseGraphView.DragPerform(evt, selectionList, dropTarget, dragSource);
            }

            IGTFVariableDeclarationModel varModelToCreate = variablesToCreate.Single().Item1;

            Store.Dispatch(new CreateVariableNodesAction(varModelToCreate, evt.mousePosition, edgesToDelete, PortModel, autoAlign: true));

            VseGraphView.ClearPlaceholdersAfterDrag();

            return true;
        }

        public bool DragEnter(DragEnterEvent evt, IEnumerable<ISelectableGraphElement> selection, IDropTarget enteredTarget, ISelection dragSource)
        {
            AddToClassList(k_DropHighlightClass);
            var dragSelection = selection.ToList();
            if (dragSelection.Count == 1 && dragSelection[0] is Token token)
                token.SetMovable(false);
            return true;
        }

        public bool DragLeave(DragLeaveEvent evt, IEnumerable<ISelectableGraphElement> selection, IDropTarget leftTarget, ISelection dragSource)
        {
            RemoveFromClassList(k_DropHighlightClass);
            var dragSelection = selection.ToList();
            if (dragSelection.Count == 1 && dragSelection[0] is Token token)
                token.SetMovable(true);
            return true;
        }

        public bool DragExited()
        {
            RemoveFromClassList(k_DropHighlightClass);
            VseGraphView?.ClearPlaceholdersAfterDrag();
            return false;
        }
    }
}
