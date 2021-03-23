using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    public class GraphChangeList : IGraphChangeList
    {
        public List<IEdgeModel> DeleteEdgeModels { get; set; } = new List<IEdgeModel>();
        public List<IGraphElementModel> ChangedElements { get; } = new List<IGraphElementModel>();
        public List<IGraphElementModel> ModelsToAutoAlign { get; } = new List<IGraphElementModel>();
        public int DeletedElements { get; set; }
        public bool BlackBoardChanged { get; set; }
        public bool RequiresRebuild { get; set; }

        public bool HasAnyTopologyChange()
        {
            return BlackBoardChanged || DeletedElements > 0 || ChangedElements.Any();
        }
    }
}
