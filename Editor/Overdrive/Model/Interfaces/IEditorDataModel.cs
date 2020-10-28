using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public enum RequestCompilationOptions
    {
        Default,
        SaveGraph,
    }

    public interface IEditorDataModel
    {
        UpdateFlags UpdateFlags { get; }
        void SetUpdateFlag(UpdateFlags flag);
        IEnumerable<IGraphElementModel> ModelsToUpdate { get; }
        Preferences Preferences { get; }
        IPluginRepository PluginRepository { get; }
        bool TracingEnabled { get; set; }
        bool CompilationPending { get; set; }
        List<OpenedGraph> PreviousGraphModels { get; }
        GameObject BoundObject { get; set; }
        IGraphElementModel ElementModelToRename { get; set; }
        void AddModelToUpdate(IGraphElementModel controller);
        void ClearModelsToUpdate();

        // PF FIXME: replace by action
        void RequestCompilation(RequestCompilationOptions options);

        // PF FIXME: smells like a hack
        bool ShouldSelectElementUponCreation(IGraphElementModel model);
        void SelectElementsUponCreation(IEnumerable<IGraphElementModel> graphElementModels, bool select);
        void ClearElementsToSelectUponCreation();
    }
}
