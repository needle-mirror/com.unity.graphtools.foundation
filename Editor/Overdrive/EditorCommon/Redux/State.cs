using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public enum UIRebuildType
    {
        None,
        Partial,
        Complete,
    }

    public class State : IDisposable
    {
        bool m_Disposed;

        protected readonly GUID m_GraphViewEditorWindowGUID;

        UIRebuildType m_UIRebuildType;
        uint m_ChangeListVersion;
        HashSet<IGraphElementModel> m_NewModels = new HashSet<IGraphElementModel>();
        HashSet<IGraphElementModel> m_ChangedModels = new HashSet<IGraphElementModel>();
        HashSet<IGraphElementModel> m_DeletedModels = new HashSet<IGraphElementModel>();
        HashSet<IGraphElementModel> m_ModelsToAutoAlign = new HashSet<IGraphElementModel>();

        PersistedEditorState m_EditorState;
        BlackboardViewStateComponent m_BlackboardViewStateComponent;
        WindowStateComponent m_WindowStateComponent;
        SelectionStateComponent m_SelectionStateComponent;
        TracingStateComponent m_TracingStateComponent;
        CompilationStateComponent m_CompilationStateComponent;

        public uint Version { get; private set; }
        public IEnumerable<IGraphElementModel> NewModels => m_NewModels;
        public IEnumerable<IGraphElementModel> ChangedModels => m_ChangedModels;
        public IEnumerable<IGraphElementModel> DeletedModels => m_DeletedModels;
        public IEnumerable<IGraphElementModel> ModelsToAutoAlign => m_ModelsToAutoAlign;

        internal UIRebuildType LastActionUIRebuildType { get; private set; }
        internal string LastDispatchedActionName { get; private set; }

        public IGraphAssetModel AssetModel => WindowState.CurrentGraph.GraphAssetModel;
        // Virtual for asset-less tests only
        public virtual IGraphModel GraphModel => AssetModel?.GraphModel;
        // Virtual for asset-less tests only
        public virtual IBlackboardGraphModel BlackboardGraphModel => AssetModel?.BlackboardGraphModel;

        protected PersistedEditorState EditorState
        {
            get => m_EditorState;
            private set
            {
                // Reset local caches
                m_BlackboardViewStateComponent = null;
                m_WindowStateComponent = null;
                m_SelectionStateComponent = null;
                m_TracingStateComponent = null;
                m_CompilationStateComponent = null;

                m_EditorState = value;
            }
        }

        public BlackboardViewStateComponent BlackboardViewState =>
            m_BlackboardViewStateComponent ??
            (m_BlackboardViewStateComponent = EditorState.GetOrCreateAssetStateComponent<BlackboardViewStateComponent>());

        public WindowStateComponent WindowState =>
            m_WindowStateComponent ??
            (m_WindowStateComponent = EditorState.GetOrCreateViewStateComponent<WindowStateComponent>(m_GraphViewEditorWindowGUID));

        public SelectionStateComponent SelectionStateComponent =>
            m_SelectionStateComponent ??
            (m_SelectionStateComponent = EditorState.GetOrCreateAssetViewStateComponent<SelectionStateComponent>(m_GraphViewEditorWindowGUID));

        public TracingStateComponent TracingState =>
            m_TracingStateComponent ??
            (m_TracingStateComponent = EditorState.GetOrCreateAssetViewStateComponent<TracingStateComponent>(m_GraphViewEditorWindowGUID));

        public CompilationStateComponent CompilationStateComponent =>
            m_CompilationStateComponent ??
            (m_CompilationStateComponent = EditorState.GetOrCreateAssetViewStateComponent<CompilationStateComponent>(m_GraphViewEditorWindowGUID));

        public Preferences Preferences { get; }

        public PluginRepository PluginRepository { get; }

        public State(GUID graphViewEditorWindowGUID, Preferences preferences)
        {
            m_GraphViewEditorWindowGUID = graphViewEditorWindowGUID;
            Preferences = preferences;
            PluginRepository = new PluginRepository();

            Version = 1;
            m_ChangeListVersion = 0;
            m_UIRebuildType = UIRebuildType.None;

            LoadGraphAsset(null, null);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_Disposed)
                return;

            // Dispose of unmanaged resources here

            if (disposing)
            {
                // Dispose of managed resources here.
                // Call members' Dispose()

                UnloadCurrentGraphAsset();

                PluginRepository?.Dispose();

                TracingState.DebuggingData = null;
            }

            m_Disposed = true;
        }

        ~State()
        {
            Dispose(false);
        }

        public void MarkNew(IEnumerable<IGraphElementModel> models)
        {
            foreach (var model in models ?? Enumerable.Empty<IGraphElementModel>())
            {
                if (model == null || m_DeletedModels.Contains(model))
                    continue;

                m_ChangedModels.Remove(model);
                m_NewModels.Add(model);

                if (m_UIRebuildType == UIRebuildType.None)
                    m_UIRebuildType = UIRebuildType.Partial;
            }
        }

        public void MarkChanged(IEnumerable<IGraphElementModel> models)
        {
            foreach (var model in models ?? Enumerable.Empty<IGraphElementModel>())
            {
                if (model == null || m_NewModels.Contains(model) || m_DeletedModels.Contains(model))
                    continue;

                m_ChangedModels.Add(model);

                if (m_UIRebuildType == UIRebuildType.None)
                    m_UIRebuildType = UIRebuildType.Partial;
            }
        }

        public void MarkDeleted(IEnumerable<IGraphElementModel> models)
        {
            foreach (var model in models ?? Enumerable.Empty<IGraphElementModel>())
            {
                if (model == null)
                    continue;

                m_NewModels.Remove(model);
                m_ChangedModels.Remove(model);

                m_DeletedModels.Add(model);
                if (m_UIRebuildType == UIRebuildType.None)
                    m_UIRebuildType = UIRebuildType.Partial;
            }
        }

        public void MarkModelToAutoAlign(IGraphElementModel model)
        {
            m_ModelsToAutoAlign.Add(model);
        }

        internal void ResetChangeList()
        {
            m_NewModels.Clear();
            m_ChangedModels.Clear();
            m_DeletedModels.Clear();
            m_ModelsToAutoAlign.Clear();
            m_UIRebuildType = UIRebuildType.None;
            m_ChangeListVersion = Version;
        }

        internal void IncrementVersion()
        {
            // unchecked: wrap around on overflow without exception.
            unchecked
            {
                Version++;
            }
        }

        public virtual UIRebuildType GetUpdateType(uint viewVersion)
        {
            // If view is new or too old, tell it to rebuild itself completely.
            if (viewVersion == 0 || viewVersion < m_ChangeListVersion)
            {
                LastActionUIRebuildType = UIRebuildType.Complete;
            }
            else
            {
                // This is safe even if Version wraps around after an overflow.
                LastActionUIRebuildType = viewVersion == Version ? UIRebuildType.None : m_UIRebuildType;
            }

            return LastActionUIRebuildType;
        }

        public virtual void PreDispatchAction(BaseAction action)
        {
            LastDispatchedActionName = action.GetType().Name;
            LastActionUIRebuildType = UIRebuildType.None;
        }

        public virtual void PostDispatchAction(BaseAction action)
        {
        }

        public virtual void PushUndo(BaseAction action)
        {
            var obj = AssetModel as Object;
            if (obj)
            {
                Undo.RegisterCompleteObjectUndo(obj, action.UndoString ?? "");
                EditorUtility.SetDirty(obj);
            }
        }

        public void RequestUIRebuild()
        {
            m_UIRebuildType = UIRebuildType.Complete;
        }

        public void LoadGraphAsset(IGraphAssetModel assetModel, GameObject boundObject)
        {
            PersistedEditorState.Flush();

            var assetPath = assetModel == null ? "" : AssetDatabase.GetAssetPath(assetModel as Object);
            EditorState = new PersistedEditorState(assetPath);
            WindowState.CurrentGraph = new OpenedGraph(assetModel, boundObject);
        }

        public void UnloadCurrentGraphAsset()
        {
            var currentAssetModel = AssetModel;
            LoadGraphAsset(null, null);
            currentAssetModel?.Dispose();
            PluginRepository?.UnregisterPlugins();
        }
    }
}
