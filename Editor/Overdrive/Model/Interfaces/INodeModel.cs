using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface INodeModel : IGraphElementModel, IMovable, IDestroyable
    {
        Color Color { get; set; }
        bool AllowSelfConnect { get; }
        bool HasUserColor { get; set; }
        bool HasProgress { get; }
        string IconTypeString { get; }
        ModelState State { get; set; }
        string Tooltip { get; }

        IEnumerable<IEdgeModel> GetConnectedEdges();

        void DefineNode();
        void OnDuplicateNode(INodeModel sourceNode);
    }
}
