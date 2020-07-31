using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;

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
        int UpdateCounter { get; set; }
        bool TracingEnabled { get; set; }
        bool CompilationPending { get; set; }
        List<OpenedGraph> PreviousGraphModels { get; }
        void AddModelToUpdate(IGTFGraphElementModel controller);
        void ClearModelsToUpdate();

        // PF FIXME: replace by action
        void RequestCompilation(RequestCompilationOptions options);

        // PF FIXME: this looks like a hack for PanToNode
        GUID NodeToFrameGuid { get; set; }

        // PF FIXME: smells like a hack
        bool ShouldSelectElementUponCreation(IGraphElement hasGraphElementModel);
        void SelectElementsUponCreation(IEnumerable<IGTFGraphElementModel> graphElementModels, bool select);
        void ClearElementsToSelectUponCreation();
    }
}
