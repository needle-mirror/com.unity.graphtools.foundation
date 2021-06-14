using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// State of the graph tool.
    /// </summary>
    public class GraphToolState : State
    {
        bool m_Disposed;

        /// <summary>
        /// The GUID of the window that displays this state.
        /// </summary>
        protected readonly Hash128 m_GraphViewEditorWindowGUID;

        BlackboardViewStateComponent m_BlackboardViewStateComponent;
        WindowStateComponent m_WindowStateComponent;
        GraphViewStateComponent m_GraphViewStateComponent;
        SelectionStateComponent m_SelectionStateComponent;
        TracingStatusStateComponent m_TracingStatusStateComponent;
        TracingControlStateComponent m_TracingControlStateComponent;
        TracingDataStateComponent m_TracingDataStateComponent;
        GraphProcessingStateComponent m_GraphProcessingStateComponent;
        ModelInspectorStateComponent m_ModelInspectorStateComponent;

        private protected virtual WindowStateComponent CreateWindowStateComponent(Hash128 guid)
        {
            return PersistedState.GetOrCreateViewStateComponent<WindowStateComponent>(guid, nameof(WindowState));
        }

        private protected virtual GraphViewStateComponent CreateGraphViewStateComponent()
        {
            return PersistedState.GetOrCreateAssetStateComponent<GraphViewStateComponent>(nameof(GraphViewState));
        }

        /// <summary>
        /// The blackboard view state component. Holds data related to what is displayed in the <see cref="Blackboard"/>.
        /// </summary>
        public BlackboardViewStateComponent BlackboardViewState =>
            m_BlackboardViewStateComponent ??= PersistedState.GetOrCreateAssetStateComponent<BlackboardViewStateComponent>(nameof(BlackboardViewState));

        /// <summary>
        /// The window state component. Holds data related to the window.
        /// </summary>
        public WindowStateComponent WindowState => m_WindowStateComponent ??= CreateWindowStateComponent(m_GraphViewEditorWindowGUID);

        /// <summary>
        /// The graph view state component. Holds data related to what is displayed in the <see cref="GraphView"/>.
        /// </summary>
        public GraphViewStateComponent GraphViewState => m_GraphViewStateComponent ??= CreateGraphViewStateComponent();

        /// <summary>
        /// The selection state component. Holds data related to the selection.
        /// </summary>
        public SelectionStateComponent SelectionState =>
            m_SelectionStateComponent ??= PersistedState.GetOrCreateAssetViewStateComponent<SelectionStateComponent>(m_GraphViewEditorWindowGUID, nameof(SelectionState));

        /// <summary>
        /// The tracing status state component. Holds data to say whether tracing is enabled or not.
        /// </summary>
        public TracingStatusStateComponent TracingStatusState =>
            m_TracingStatusStateComponent ??= PersistedState.GetOrCreateAssetViewStateComponent<TracingStatusStateComponent>(m_GraphViewEditorWindowGUID, nameof(TracingStatusState));

        /// <summary>
        /// The tracing control state component. Holds data to control tracing and debugging.
        /// </summary>
        public TracingControlStateComponent TracingControlState =>
            m_TracingControlStateComponent ??= PersistedState.GetOrCreateAssetViewStateComponent<TracingControlStateComponent>(m_GraphViewEditorWindowGUID, nameof(TracingControlState));

        /// <summary>
        /// The tracing data state component. Holds data related to tracing and debugging.
        /// </summary>
        public TracingDataStateComponent TracingDataState =>
            m_TracingDataStateComponent ??= PersistedState.GetOrCreateAssetViewStateComponent<TracingDataStateComponent>(m_GraphViewEditorWindowGUID, nameof(TracingDataState));

        /// <summary>
        /// The graph processing state component. Holds data related to graph processing.
        /// </summary>
        public GraphProcessingStateComponent GraphProcessingState =>
            m_GraphProcessingStateComponent ??= PersistedState.GetOrCreateAssetStateComponent<GraphProcessingStateComponent>(nameof(GraphProcessingState));

        public ModelInspectorStateComponent ModelInspectorState =>
            m_ModelInspectorStateComponent ??= PersistedState.GetOrCreateViewStateComponent<ModelInspectorStateComponent>(m_GraphViewEditorWindowGUID, nameof(ModelInspectorState));

        /// <inheritdoc />
        public override IEnumerable<IStateComponent> AllStateComponents
        {
            get
            {
                yield return BlackboardViewState;
                yield return WindowState;
                yield return GraphViewState;
                yield return SelectionState;
                yield return TracingStatusState;
                yield return TracingControlState;
                yield return TracingDataState;
                yield return GraphProcessingState;
                yield return ModelInspectorState;
            }
        }

        /// <summary>
        /// The tool preferences.
        /// </summary>
        public Preferences Preferences { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphToolState" /> class.
        /// </summary>
        /// <param name="graphViewEditorWindowGUID">The GUID of the window displaying this state.</param>
        /// <param name="preferences">The tool preferences.</param>
        public GraphToolState(Hash128 graphViewEditorWindowGUID, Preferences preferences)
        {
            m_GraphViewEditorWindowGUID = graphViewEditorWindowGUID;
            Preferences = preferences;
        }

        ~GraphToolState()
        {
            Dispose(false);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (m_Disposed)
                return;

            // Dispose of unmanaged resources here

            if (disposing)
            {
                // Dispose of managed resources here

                LoadGraphAsset(null, null);
            }

            m_Disposed = true;
            base.Dispose();
        }

        /// <inheritdoc />
        public override void RegisterCommandHandlers(Dispatcher dispatcher)
        {
            base.RegisterCommandHandlers(dispatcher);

            if (!(dispatcher is CommandDispatcher commandDispatcher))
                return;

            commandDispatcher.RegisterCommandHandler<UndoRedoCommand>(UndoRedoCommandHandler);

            commandDispatcher.RegisterCommandHandler<CreateNodeFromPortCommand>(CreateNodeFromPortCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<CreateEdgeCommand>(CreateEdgeCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<AddControlPointOnEdgeCommand>(AddControlPointOnEdgeCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<MoveEdgeControlPointCommand>(MoveEdgeControlPointCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<RemoveEdgeControlPointCommand>(RemoveEdgeControlPointCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<SetEdgeEditModeCommand>(SetEdgeEditModeCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<ReorderEdgeCommand>(ReorderEdgeCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<SplitEdgeAndInsertExistingNodeCommand>(SplitEdgeAndInsertExistingNodeCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<CreateNodeOnEdgeCommand>(CreateNodeOnEdgeCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<ConvertEdgesToPortalsCommand>(ConvertEdgesToPortalsCommand.DefaultCommandHandler);

            commandDispatcher.RegisterCommandHandler<DisconnectNodeCommand>(DisconnectNodeCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<CreateNodeFromSearcherCommand>(CreateNodeFromSearcherCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<ChangeNodeStateCommand>(ChangeNodeStateCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<CollapseNodeCommand>(CollapseNodeCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<UpdateConstantValueCommand>(UpdateConstantValueCommand.DefaultCommandHandler);

            commandDispatcher.RegisterCommandHandler<CreateOppositePortalCommand>(CreateOppositePortalCommand.DefaultCommandHandler);

            commandDispatcher.RegisterCommandHandler<DeleteEdgeCommand>(DeleteEdgeCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<BuildAllEditorCommand>(BuildAllEditorCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<PasteSerializedDataCommand>(PasteSerializedDataCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<ReframeGraphViewCommand>(ReframeGraphViewCommand.DefaultCommandHandler);

            commandDispatcher.RegisterCommandHandler<AlignNodesCommand>(AlignNodesCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<RenameElementCommand>(RenameElementCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<DeleteElementsCommand>(DeleteElementsCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<UpdatePortConstantCommand>(UpdatePortConstantCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<BypassNodesCommand>(BypassNodesCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<MoveElementsCommand>(MoveElementsCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<AutoPlaceElementsCommand>(AutoPlaceElementsCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<ChangeElementColorCommand>(ChangeElementColorCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<ResetElementColorCommand>(ResetElementColorCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<ChangeElementLayoutCommand>(ChangeElementLayoutCommand.DefaultCommandHandler);

            commandDispatcher.RegisterCommandHandler<CreatePlacematCommand>(CreatePlacematCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<ChangePlacematZOrdersCommand>(ChangePlacematZOrdersCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<CollapsePlacematCommand>(CollapsePlacematCommand.DefaultCommandHandler);

            commandDispatcher.RegisterCommandHandler<CreateStickyNoteCommand>(CreateStickyNoteCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<UpdateStickyNoteCommand>(UpdateStickyNoteCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<UpdateStickyNoteThemeCommand>(UpdateStickyNoteThemeCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<UpdateStickyNoteTextSizeCommand>(UpdateStickyNoteTextSizeCommand.DefaultCommandHandler);

            commandDispatcher.RegisterCommandHandler<CreateVariableNodesCommand>(CreateVariableNodesCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<CreateGraphVariableDeclarationCommand>(CreateGraphVariableDeclarationCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<ReorderGraphVariableDeclarationCommand>(ReorderGraphVariableDeclarationCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<ConvertConstantNodesAndVariableNodesCommand>(ConvertConstantNodesAndVariableNodesCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<ItemizeNodeCommand>(ItemizeNodeCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<LockConstantNodeCommand>(LockConstantNodeCommand.DefaultCommandHandler);

            commandDispatcher.RegisterCommandHandler<InitializeVariableCommand>(InitializeVariableCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<ChangeVariableTypeCommand>(ChangeVariableTypeCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<ExposeVariableCommand>(ExposeVariableCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<UpdateTooltipCommand>(UpdateTooltipCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<CollapseVariableInBlackboard>(CollapseVariableInBlackboard.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<ChangeVariableDeclarationCommand>(ChangeVariableDeclarationCommand.DefaultCommandHandler);

            commandDispatcher.RegisterCommandHandler<ActivateTracingCommand>(ActivateTracingCommand.DefaultCommandHandler);

            commandDispatcher.RegisterCommandHandler<SelectElementsCommand>(SelectElementsCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<ClearSelectionCommand>(ClearSelectionCommand.DefaultCommandHandler);

            commandDispatcher.RegisterCommandHandler<LoadGraphAssetCommand>(LoadGraphAssetCommand.DefaultCommandHandler);

            commandDispatcher.RegisterCommandHandler<SetModelFieldCommand>(SetModelFieldCommand.DefaultCommandHandler);
        }

        /// <inheritdoc />
        protected override void ResetStateCaches()
        {
            base.ResetStateCaches();

            m_BlackboardViewStateComponent = null;
            m_WindowStateComponent = null;
            m_GraphViewStateComponent = null;
            m_SelectionStateComponent = null;
            m_TracingControlStateComponent = null;
            m_TracingDataStateComponent = null;
            m_GraphProcessingStateComponent = null;
            m_ModelInspectorStateComponent = null;
        }

        /// <inheritdoc />
        public override void PushUndo(UndoableCommand command)
        {
            var obj = WindowState.AssetModel as Object;
            if (obj != null)
            {
                Undo.RegisterCompleteObjectUndo(new[] { obj }, command?.UndoString ?? "");
            }

            base.PushUndo(command);
        }

        /// <inheritdoc />
        protected override void SerializeForUndo(SerializedReferenceDictionary<string, string> stateComponentUndoData)
        {
            base.SerializeForUndo(stateComponentUndoData);

            stateComponentUndoData.Add(nameof(SelectionState), StateComponentHelper.Serialize(SelectionState));
            stateComponentUndoData.Add(nameof(GraphViewState), StateComponentHelper.Serialize(GraphViewState));
            stateComponentUndoData.Add(nameof(BlackboardViewState), StateComponentHelper.Serialize(BlackboardViewState));
            stateComponentUndoData.Add(nameof(WindowState), StateComponentHelper.Serialize(WindowState));
        }

        /// <inheritdoc />
        protected override void DeserializeFromUndo(SerializedReferenceDictionary<string, string> stateComponentUndoData)
        {
            base.DeserializeFromUndo(stateComponentUndoData);

            if (stateComponentUndoData.TryGetValue(nameof(SelectionState), out var serializedData))
            {
                var newSelectionState = StateComponentHelper.Deserialize<SelectionStateComponent>(serializedData);
                PersistedState.SetAssetViewStateComponent(m_GraphViewEditorWindowGUID, newSelectionState);
            }

            if (stateComponentUndoData.TryGetValue(nameof(GraphViewState), out serializedData))
            {
                var newGraphViewState = StateComponentHelper.Deserialize<GraphViewStateComponent>(serializedData);
                PersistedState.SetAssetStateComponent(newGraphViewState);
            }

            if (stateComponentUndoData.TryGetValue(nameof(BlackboardViewState), out serializedData))
            {
                var newBlackboardState = StateComponentHelper.Deserialize<BlackboardViewStateComponent>(serializedData);
                PersistedState.SetAssetStateComponent(newBlackboardState);
            }

            if (stateComponentUndoData.TryGetValue(nameof(WindowState), out serializedData))
            {
                var newWindowState = StateComponentHelper.Deserialize<WindowStateComponent>(serializedData);
                PersistedState.SetAssetStateComponent(newWindowState);
            }
        }

        /// <inheritdoc />
        protected override void ValidateAfterDeserialize()
        {
            SelectionState.ValidateAfterDeserialize();
            GraphViewState.ValidateAfterDeserialize();
            BlackboardViewState.ValidateAfterDeserialize();
            WindowState.ValidateAfterDeserialize();
        }

        /// <summary>
        /// Loads the graph asset model and the persisted state associated with the asset.
        /// </summary>
        /// <param name="assetModel">The graph asset model to load.</param>
        /// <param name="boundObject">The GameObject to which the graph is bound, if any.</param>
        public void LoadGraphAsset(IGraphAssetModel assetModel, GameObject boundObject)
        {
            ResetStateCaches();

            if (assetModel != null)
            {
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(assetModel as Object, out var guid, out long fileId))
                {
                    guid = (assetModel as Object)?.GetInstanceID().ToString() ?? "0";
                    fileId = 0;
                }
                PersistedState.SetAssetKey(guid + "/" + fileId);
            }
            else
            {
                PersistedState.SetAssetKey("");
            }

            using (var windowStateUpdater = WindowState.UpdateScope)
            {
                windowStateUpdater.LoadGraphAsset(assetModel, boundObject);
            }

            using (var graphViewStateUpdater = GraphViewState.UpdateScope)
            {
                graphViewStateUpdater.LoadGraphAsset(assetModel);
            }
        }

        /// <summary>
        /// Notifies the relevant state components that an asset was modified outside of the tool.
        /// </summary>
        /// <param name="assetGUID">The GUID of the modified graph asset.</param>
        public void GraphAssetChanged(string assetGUID)
        {
            if (WindowState.CurrentGraph.GraphModelAssetGUID == assetGUID ||
                WindowState.SubGraphStack.Any(og => og.GraphModelAssetGUID == assetGUID))
            {
                using (var updater = WindowState.UpdateScope)
                {
                    updater.AssetChangedOnDisk();
                }
            }
            if (GraphViewState.AssetModelGUID == assetGUID)
            {
                using (var updater = GraphViewState.UpdateScope)
                {
                    updater.AssetChangedOnDisk();
                }
            }
        }

        /// <summary>
        /// Handler for the UndoRedoCommand.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="command">The command.</param>
        public static void UndoRedoCommandHandler(GraphToolState state, UndoRedoCommand command)
        {
            UndoRedoCommand.DefaultCommandHandler(state, command);
            var graphModel = state.WindowState.GraphModel;
            graphModel?.UndoRedoPerformed();
        }
    }
}
