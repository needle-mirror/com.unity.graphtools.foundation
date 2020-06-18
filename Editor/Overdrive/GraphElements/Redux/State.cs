using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
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
        public virtual IGTFGraphModel GraphModel => AssetModel?.GraphModel;
        public IGTFGraphAssetModel AssetModel { get; set; }

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                EditorDataModel = null;
            }
        }

        ~State()
        {
            Dispose(false);
        }

        public void MarkForUpdate(UpdateFlags flag, IGTFGraphElementModel model = null)
        {
            EditorDataModel?.SetUpdateFlag(flag);
            if (model != null)
            {
                EditorDataModel?.AddModelToUpdate(model);
            }
        }

        public IGTFEditorDataModel EditorDataModel { get; protected set; }
    }
}
