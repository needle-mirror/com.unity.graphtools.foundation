using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The state of the tool.
    /// </summary>
    public class GraphToolState : IDisposable
    {
        bool m_Disposed;

        /// <summary>
        /// The GUID of the window that displays this state.
        /// </summary>
        protected readonly GUID m_GraphViewEditorWindowGUID;

        PersistedEditorState m_PersistedState;
        BlackboardViewStateComponent m_BlackboardViewStateComponent;
        WindowStateComponent m_WindowStateComponent;
        GraphViewStateComponent m_GraphViewStateComponent;
        SelectionStateComponent m_SelectionStateComponent;
        TracingControlStateComponent m_TracingControlStateComponent;
        TracingDataStateComponent m_TracingDataStateComponent;
        GraphProcessingStateComponent m_GraphProcessingStateComponent;

        GraphToolStateUndo m_GraphToolStateUndo;

        internal string LastDispatchedCommandName { get; private set; }

        /// <summary>
        /// The persisted state.
        /// </summary>
        protected PersistedEditorState PersistedState
        {
            get => m_PersistedState;
            private set
            {
                ResetStateCaches();
                m_PersistedState = value;
            }
        }

        private protected virtual WindowStateComponent CreateWindowStateComponent(GUID guid)
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
            m_BlackboardViewStateComponent ??
            (m_BlackboardViewStateComponent = PersistedState.GetOrCreateAssetStateComponent<BlackboardViewStateComponent>(nameof(BlackboardViewState)));

        /// <summary>
        /// The window state component. Holds data related to the window.
        /// </summary>
        public WindowStateComponent WindowState =>
            m_WindowStateComponent ??
            (m_WindowStateComponent = CreateWindowStateComponent(m_GraphViewEditorWindowGUID));

        /// <summary>
        /// The graph view state component. Holds data related to what is displayed in the <see cref="GraphView"/>.
        /// </summary>
        public GraphViewStateComponent GraphViewState =>
            m_GraphViewStateComponent ?? (m_GraphViewStateComponent = CreateGraphViewStateComponent());

        /// <summary>
        /// The selection state component. Holds data related to the selection.
        /// </summary>
        public SelectionStateComponent SelectionState =>
            m_SelectionStateComponent ??
            (m_SelectionStateComponent = PersistedState.GetOrCreateAssetViewStateComponent<SelectionStateComponent>(m_GraphViewEditorWindowGUID, nameof(SelectionState)));

        /// <summary>
        /// The tracing control state component. Holds data to control tracing and debugging.
        /// </summary>
        public TracingControlStateComponent TracingControlState =>
            m_TracingControlStateComponent ??
            (m_TracingControlStateComponent = PersistedState.GetOrCreateAssetViewStateComponent<TracingControlStateComponent>(m_GraphViewEditorWindowGUID, nameof(TracingControlState)));

        /// <summary>
        /// The tracing data state component. Holds data related to tracing and debugging.
        /// </summary>
        public TracingDataStateComponent TracingDataState =>
            m_TracingDataStateComponent ??
            (m_TracingDataStateComponent = PersistedState.GetOrCreateAssetViewStateComponent<TracingDataStateComponent>(m_GraphViewEditorWindowGUID, nameof(TracingDataState)));

        /// <summary>
        /// The graph processing state component. Holds data related to graph processing.
        /// </summary>
        public GraphProcessingStateComponent GraphProcessingState =>
            m_GraphProcessingStateComponent ??
            (m_GraphProcessingStateComponent = PersistedState.GetOrCreateAssetStateComponent<GraphProcessingStateComponent>(nameof(GraphProcessingState)));

        /// <summary>
        /// All the state components.
        /// </summary>
        public virtual IEnumerable<IStateComponent> AllStateComponents
        {
            get
            {
                yield return BlackboardViewState;
                yield return WindowState;
                yield return GraphViewState;
                yield return SelectionState;
                yield return TracingControlState;
                yield return TracingDataState;
                yield return GraphProcessingState;
            }
        }

        /// <summary>
        /// The tool preferences.
        /// </summary>
        public Preferences Preferences { get; }

        /// <summary>
        /// Initializes a new instance of the GraphToolState class.
        /// </summary>
        /// <param name="graphViewEditorWindowGUID">The GUID of the window displaying this state.</param>
        /// <param name="preferences">The tool preferences.</param>
        public GraphToolState(GUID graphViewEditorWindowGUID, Preferences preferences)
        {
            m_GraphViewEditorWindowGUID = graphViewEditorWindowGUID;
            Preferences = preferences;
            PersistedState = new PersistedEditorState("");
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of resources used by the state.
        /// </summary>
        /// <param name="disposing">When true, this method is called from IDisposable.Dispose.
        /// Otherwise it is called from the finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (m_Disposed)
                return;

            // Dispose of unmanaged resources here

            if (disposing)
            {
                // Dispose of managed resources here

                LoadGraphAsset(null, null);

                foreach (var stateComponent in AllStateComponents)
                {
                    stateComponent.Dispose();
                }
            }

            m_Disposed = true;
        }

        ~GraphToolState()
        {
            Dispose(false);
        }

        void ResetStateCaches()
        {
            m_BlackboardViewStateComponent = null;
            m_WindowStateComponent = null;
            m_GraphViewStateComponent = null;
            m_SelectionStateComponent = null;
            m_TracingControlStateComponent = null;
            m_TracingDataStateComponent = null;
            m_GraphProcessingStateComponent = null;
        }

        /// <summary>
        /// Called when a command is dispatched, before the command handler is executed.
        /// </summary>
        /// <param name="command"></param>
        protected internal virtual void PreDispatchCommand(Command command)
        {
            LastDispatchedCommandName = command.GetType().Name;
        }

        /// <summary>
        /// Called when a command is dispatched, after the command handler has been executed.
        /// </summary>
        /// <param name="command"></param>
        protected internal virtual void PostDispatchCommand(Command command)
        {
        }

        /// <summary>
        /// Finishes the previous undo group, if there was one, and begin a new undo group.
        /// The name of the group is taken from the command argument.
        /// </summary>
        /// <remarks>
        /// The undo group is automatically incremented based on events
        /// eg. mouse down events, executing a menu item increments the current
        /// group. But sometimes it is necessary to manually start a new group of undo operations.
        /// </remarks>
        /// <param name="command">The command that causes the new undo group to be started.
        /// Pass null if the undo group should not be named.</param>
        public void PushUndoGroup(Command command = null)
        {
            Undo.IncrementCurrentGroup();
            if (command != null && !string.IsNullOrEmpty(command.UndoString))
                Undo.SetCurrentGroupName(command.UndoString);
        }

        /// <summary>
        /// Pushes the current state on the undo stack.
        /// </summary>
        /// <param name="command">Use command.UndoString as the name of the undo item.</param>
        public virtual void PushUndo(Command command)
        {
            if (m_GraphToolStateUndo == null)
            {
                m_GraphToolStateUndo = ScriptableObject.CreateInstance<GraphToolStateUndo>();
                m_GraphToolStateUndo.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector;
                m_GraphToolStateUndo.State = this;
            }

            var obj = WindowState.AssetModel as Object;
            if (obj != null)
            {
                // We need to push two separate undos to get the undo type right and avoid dirtying the scene.
                Undo.RegisterCompleteObjectUndo(new[] { obj }, command?.UndoString ?? "");
                Undo.RegisterCompleteObjectUndo(new Object[] { m_GraphToolStateUndo }, command?.UndoString ?? "");
            }
            else
            {
                Undo.RegisterCompleteObjectUndo(new Object[] { m_GraphToolStateUndo }, command?.UndoString ?? "");
            }
        }

        /// <summary>
        /// Serializes the state to push on the undo stack.
        /// </summary>
        /// <param name="stateComponentUndoData">A list to hold the serialized data.</param>
        protected internal virtual void SerializeForUndo(Dictionary<string, string> stateComponentUndoData)
        {
            stateComponentUndoData.Add(nameof(SelectionState), StateComponentHelper.Serialize(SelectionState));
            stateComponentUndoData.Add(nameof(GraphViewState), StateComponentHelper.Serialize(GraphViewState));
            stateComponentUndoData.Add(nameof(BlackboardViewState), StateComponentHelper.Serialize(BlackboardViewState));
        }

        /// <summary>
        /// Restores the state from its serialized data.
        /// </summary>
        /// <param name="stateComponentUndoData">The serialized state data.</param>
        protected internal virtual void DeserializeFromUndo(Dictionary<string, string> stateComponentUndoData)
        {
            ResetStateCaches();

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
        }

        static string GetAssetPath(IGraphAssetModel asset)
        {
            string assetPath;
            if (asset == null)
            {
                assetPath = "";
            }
            else
            {
                assetPath = AssetDatabase.GetAssetPath(asset as Object);
                if (string.IsNullOrEmpty(assetPath))
                {
                    assetPath = "InMemoryAsset/" + asset.Name + "/" + asset.GetHashCode();
                }
            }

            return assetPath;
        }

        /// <summary>
        /// Loads the graph asset model and the persisted state associated with the asset.
        /// </summary>
        /// <param name="assetModel">The graph asset model to load.</param>
        /// <param name="boundObject">The GameObject to which the graph is bound, if any.</param>
        public void LoadGraphAsset(IGraphAssetModel assetModel, GameObject boundObject)
        {
            PersistedEditorState.Flush();
            ResetStateCaches();
            var assetPath = GetAssetPath(assetModel);
            PersistedState.SetAssetKey(assetPath);

            using (var windowStateUpdater = WindowState.UpdateScope)
            {
                windowStateUpdater.LoadGraphAsset(assetModel, boundObject);
            }

            using (var graphViewStateUpdater = GraphViewState.UpdateScope)
            {
                graphViewStateUpdater.LoadGraphAsset(assetModel);
            }
        }

        internal void PurgeAllChangesets()
        {
            foreach (var stateComponent in AllStateComponents)
            {
                stateComponent.PurgeOldChangesets(uint.MaxValue);
            }
        }

        [Obsolete("2021-02-19 Use WindowState.AssetModel instead.")]
        public IGraphAssetModel AssetModel => WindowState.AssetModel;
        // Virtual for asset-less tests only
        [Obsolete("2021-02-19 Use WindowState.GraphModel instead.")]
        public virtual IGraphModel GraphModel => WindowState.GraphModel;
        // Virtual for asset-less tests only
        [Obsolete("2021-02-19 Use WindowState.BlackboardGraphModel instead.")]
        public virtual IBlackboardGraphModel BlackboardGraphModel => WindowState.BlackboardGraphModel;

        [Obsolete("2021-02-19 Use IGraphViewStateComponentUpdater.MarkNew instead.")]
        public void MarkNew(IEnumerable<IGraphElementModel> models)
        {
            using (var stateUpdater = GraphViewState.UpdateScope)
            {
                stateUpdater.MarkNew(models);
            }
        }

        [Obsolete("2021-02-19 Use IGraphViewStateComponentUpdater.MarkChanged instead.")]
        public void MarkChanged(IEnumerable<IGraphElementModel> models)
        {
            using (var stateUpdater = GraphViewState.UpdateScope)
            {
                stateUpdater.MarkChanged(models);
            }
        }

        [Obsolete("2021-02-19 Use IGraphViewStateComponentUpdater.MarkDeleted instead.")]
        public void MarkDeleted(IEnumerable<IGraphElementModel> models)
        {
            using (var stateUpdater = GraphViewState.UpdateScope)
            {
                stateUpdater.MarkDeleted(models);
            }
        }

        [Obsolete("2021-02-19 Use IGraphViewStateComponentUpdater.MarkModelToAutoAlign instead.")]
        public void MarkModelToAutoAlign(IGraphElementModel model)
        {
            using (var stateUpdater = GraphViewState.UpdateScope)
            {
                stateUpdater.MarkModelToAutoAlign(model);
            }
        }

        [Obsolete("2021-02-19 Use GraphViewState.ForceCompleteUpdate instead.")]
        public void RequestUIRebuild()
        {
            using (var stateUpdater = GraphViewState.UpdateScope)
            {
                stateUpdater.ForceCompleteUpdate();
            }
        }
    }
}
