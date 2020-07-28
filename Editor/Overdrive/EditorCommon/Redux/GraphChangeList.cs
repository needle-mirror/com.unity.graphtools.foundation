using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class GraphChangeList
    {
        public List<IGTFEdgeModel> DeletedEdges { get; set; } = new List<IGTFEdgeModel>();
        public List<IGTFGraphElementModel> ChangedElements { get; } = new List<IGTFGraphElementModel>();
        public List<IGTFGraphElementModel> ElementsToAutoAlign { get; } = new List<IGTFGraphElementModel>();
        public int DeletedElements { get; set; }
        public bool BlackBoardChanged { get; set; }
        public bool RequiresRebuild { get; set; }

        public bool HasAnyTopologyChange()
        {
            return BlackBoardChanged || DeletedElements > 0 || ChangedElements.Any();
        }
    }
}
