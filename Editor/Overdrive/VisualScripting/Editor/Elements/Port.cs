using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class Port : GraphElements.Port, IDropTarget, IBadgeContainer
    {
        const string k_DropHighlightClass = "drop-highlighted";

        VseGraphView VseGraphView => GraphView as VseGraphView;
        public new Store Store => base.Store as Store;
        public new PortModel PortModel => base.PortModel as PortModel;

        public IconBadge ErrorBadge { get; set; }
        public ValueBadge ValueBadge { get; set; }

        /// <summary>
        /// Used to highlight the port when it is triggered during tracing
        /// </summary>
        public bool ExecutionPortActive
        {
            set => EnableInClassList("execution-active", value);
        }

        static void OnDropOutsideCallback(IStore store, Vector2 pos, GraphElements.Edge edge)
        {
            VseGraphView graphView = edge.GetFirstAncestorOfType<VseGraphView>();
            Vector2 localPos = graphView.contentViewContainer.WorldToLocal(pos);

            List<IGTFEdgeModel> edgesToDelete = EdgeConnectorListener.GetDropEdgeModelsToDelete(edge.EdgeModel);

            // when grabbing an existing edge's end, the edgeModel should be deleted
            if (edge.EdgeModel != null)
                edgesToDelete.Add(edge.EdgeModel);

            IPortModel existingPortModel;
            // warning: when dragging the end of an existing edge, both ports are non null.
            if (edge.Input != null && edge.Output != null)
            {
                float distanceToOutput = Vector2.Distance(edge.From, pos);
                float distanceToInput = Vector2.Distance(edge.To, pos);
                // note: if the user was able to stack perfectly both ports, we'd be in trouble
                if (distanceToOutput < distanceToInput)
                    existingPortModel = edge.Input as IPortModel;
                else
                    existingPortModel = edge.Output as IPortModel;
            }
            else
            {
                var existingPort = (edge.Input ?? edge.Output);
                existingPortModel = existingPort as IPortModel;
            }

            ((Store)store)?.GetState().CurrentGraphModel?.Stencil.CreateNodesFromPort((Store)store, existingPortModel, localPos, pos, edgesToDelete);
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

        public static readonly string k_ConstantEditorPartName = "constant-editor";

        static readonly string k_PortTypeModifierClassNamePrefix = "ge-port--type-";
        static string GetClassNameModifierForType(PortType t)
        {
            return k_PortTypeModifierClassNamePrefix + t.ToString().ToLower();
        }

        protected override void BuildPartList()
        {
            if (!PortModel.Options.HasFlag(PortModel.PortModelOptions.Hidden))
            {
                base.BuildPartList();

                PartList.ReplacePart(k_ConnectorPartName, PortConnectorWithIconPart.Create(k_ConnectorPartName, Model, this, k_UssClassName));
                PartList.AppendPart(PortConstantEditorPart.Create(k_ConstantEditorPartName, Model, this, k_UssClassName, Store.GetState()?.EditorDataModel));
            }
        }

        protected override void BuildSelfUI()
        {
            if (!PortModel.Options.HasFlag(PortModel.PortModelOptions.Hidden))
            {
                base.BuildSelfUI();
            }
        }

        protected override void PostBuildUI()
        {
            if (!PortModel.Options.HasFlag(PortModel.PortModelOptions.Hidden))
            {
                base.PostBuildUI();

                EdgeConnector?.SetDropOutsideDelegate((s, edge, pos) => OnDropOutsideCallback(s, pos, edge));
                styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "Port.uss"));
            }
        }

        protected override void UpdateSelfFromModel()
        {
            base.UpdateSelfFromModel();

            this.PrefixRemoveFromClassList(k_PortTypeModifierClassNamePrefix);
            AddToClassList(GetClassNameModifierForType(PortModel.PortType));
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
                && token.GraphElementModel is IVariableModel varModel
                && !varModel.OutputPort.ConnectionPortModels.Any(p => p == PortModel)
                && !ReferenceEquals(PortModel.NodeModel, token.GraphElementModel);
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

            var edgesVMToDelete = PortModel.Capacity == PortCapacity.Multi ? new List<IEdgeModel>() : PortModel.ConnectedEdges;
            var edgesToDelete = edgesVMToDelete;

            if (IsTokenToDrop(selectionList[0]))
            {
                Token token = ((Token)selectionList[0]);
                token.SetMovable(true);
                Store.Dispatch(new CreateEdgeAction(PortModel, ((IVariableModel)token.GraphElementModel).OutputPort as IGTFPortModel, edgesToDelete.Cast<IGTFEdgeModel>(), CreateEdgeAction.PortAlignmentType.Input));
                return true;
            }

            List<Tuple<IVariableDeclarationModel, Vector2>> variablesToCreate = DragAndDropHelper.ExtractVariablesFromDroppedElements(dropElements, VseGraphView, evt.mousePosition);

            PortType type = PortModel.PortType;
            if (type != PortType.Data && type != PortType.Instance) // do not connect loop/exec ports to variables
            {
                return VseGraphView.DragPerform(evt, selectionList, dropTarget, dragSource);
            }

            IVariableDeclarationModel varModelToCreate = variablesToCreate.Single().Item1;

            Store.Dispatch(new CreateVariableNodesAction(varModelToCreate, evt.mousePosition, edgesToDelete.Cast<IGTFEdgeModel>(), PortModel, autoAlign: true));

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
