using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IGraphChangeList
    {
        List<IGTFEdgeModel> DeletedEdges { get; set; }

        List<IGTFGraphElementModel> ChangedElements { get; }

        List<IGTFGraphElementModel> ElementsToAutoAlign { get; }

        bool BlackBoardChanged { get; set; }

        bool RequiresRebuild { get; set; }

        bool HasAnyTopologyChange();
    }
}
