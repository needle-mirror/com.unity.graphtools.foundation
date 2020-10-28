using System;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    // PF FIXME: some VS specific flags here.
    [Flags]
    public enum UpdateFlags
    {
        None               = 0,
        Selection          = 1 << 0,
        GraphGeometry      = 1 << 1,
        GraphTopology      = 1 << 2,
        CompilationResult  = 1 << 3,
        RequestCompilation = 1 << 4,
        RequestRebuild     = 1 << 5,
        UpdateView         = 1 << 6,

        All = Selection | GraphGeometry | GraphTopology | RequestCompilation | RequestRebuild,
    }

    public class State : IDisposable
    {
        bool m_Disposed;

        public IGraphAssetModel AssetModel { get; set; }
        public virtual IGraphModel CurrentGraphModel => AssetModel?.GraphModel;
        public IEditorDataModel EditorDataModel { get; }
        public Preferences Preferences => EditorDataModel?.Preferences;

        public TracingDataModel TracingDataModel { get; }
        public ICompilationResultModel CompilationResultModel { get; private set; }

        public State(IEditorDataModel editorDataModel)
        {
            EditorDataModel = editorDataModel;
            TracingDataModel = new TracingDataModel(-1);
            CompilationResultModel = new CompilationResultModel();
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

                TracingDataModel.DebuggingData = null;
                CompilationResultModel = null;
            }

            m_Disposed = true;
        }

        ~State()
        {
            Dispose(false);
        }

        public virtual void PreStateChanged()
        {
            if (EditorDataModel != null && CurrentGraphModel.HasAnyTopologyChange())
                EditorDataModel.SetUpdateFlag(EditorDataModel.UpdateFlags | UpdateFlags.GraphTopology);

            if (EditorDataModel != null && CurrentGraphModel?.LastChanges?.RequiresRebuild == true)
                EditorDataModel.SetUpdateFlag(EditorDataModel.UpdateFlags | UpdateFlags.RequestRebuild);
        }

        public virtual void PostStateChanged()
        {
            EditorDataModel?.SetUpdateFlag(UpdateFlags.None);
        }

        public virtual void PreDispatchAction(BaseAction action)
        {
            LastDispatchedActionName = action.GetType().Name;
            CurrentGraphModel?.ResetChangeList();
            LastActionUIRebuildType = UIRebuildType.None;
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

        public void MarkForUpdate(UpdateFlags flag, IGraphElementModel model = null)
        {
            EditorDataModel?.SetUpdateFlag(flag);
            if (model != null)
            {
                EditorDataModel?.AddModelToUpdate(model);
            }
        }

        // For performance debugging purposes
        internal enum UIRebuildType
        {
            None,
            Partial,
            Full
        }
        internal UIRebuildType LastActionUIRebuildType { get; set; }
        internal string LastDispatchedActionName { get; set; }

        public void UnloadCurrentGraphAsset()
        {
            AssetModel?.Dispose();
            AssetModel = null;

            EditorDataModel?.PluginRepository?.UnregisterPlugins();
        }
    }
}
