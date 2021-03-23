using System.Collections.Generic;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    public interface IGraphChangeList
    {
        List<IEdgeModel> DeleteEdgeModels { get; set; }

        List<IGraphElementModel> ChangedElements { get; }

        List<IGraphElementModel> ModelsToAutoAlign { get; }

        bool BlackBoardChanged { get; set; }

        bool RequiresRebuild { get; }

        bool HasAnyTopologyChange();
    }
}
