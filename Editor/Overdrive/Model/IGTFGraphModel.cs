using System;
using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Model
{
    public interface IGTFGraphAssetModel : IDisposable
    {
        IGTFGraphModel GraphModel { get; }
    }

    public interface IGTFGraphModel : IDisposable
    {
        IGTFEdgeModel CreateEdgeGTF(IGTFPortModel inputPort, IGTFPortModel outputPort);
        void DeleteElements(IEnumerable<IGTFGraphElementModel> graphElementModels);
    }
}
