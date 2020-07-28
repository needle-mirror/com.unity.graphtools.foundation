using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Model
{
    public interface IGTFNodeModel : IGTFGraphElementModel, ISelectable, IPositioned, IDeletable, IDroppable, ICopiable, IDestroyable
    {
        Color Color { get; }
        bool AllowSelfConnect { get; }
        bool HasUserColor { get; }
        bool HasProgress { get; }
        string IconTypeString { get; }
        ModelState State { get; }
        string Tooltip { get; }

        IEnumerable<IGTFEdgeModel> GetConnectedEdges();

        void DefineNode();
    }
}
