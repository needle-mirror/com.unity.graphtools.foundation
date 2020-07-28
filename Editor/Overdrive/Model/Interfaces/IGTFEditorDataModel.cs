using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Model
{
    public enum RequestCompilationOptions
    {
        Default,
        SaveGraph,
    }

    public interface IGTFEditorDataModel
    {
        UpdateFlags UpdateFlags { get; }
        void SetUpdateFlag(UpdateFlags flag);
        IEnumerable<IGTFGraphElementModel> ModelsToUpdate { get; }
        Preferences Preferences { get; }
        IPluginRepository PluginRepository { get; }
        bool TracingEnabled { get; set; }
        bool CompilationPending { get; set; }
        List<OpenedGraph> PreviousGraphModels { get; }
        IGTFGraphElementModel ElementModelToRename { get; set; }
        void AddModelToUpdate(IGTFGraphElementModel controller);
        void ClearModelsToUpdate();

        // PF FIXME: replace by action
        void RequestCompilation(RequestCompilationOptions options);

        // PF FIXME: smells like a hack
        bool ShouldSelectElementUponCreation(IGTFGraphElementModel model);
        void SelectElementsUponCreation(IEnumerable<IGTFGraphElementModel> graphElementModels, bool select);
        void ClearElementsToSelectUponCreation();
    }
}
