using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Model
{
    public interface IGTFGraphAssetModel : IDisposable
    {
        string Name { get; set; }
        IGTFGraphModel GraphModel { get; }
        void CreateGraph(string graphName, Type stencilType, bool writeOnDisk = true);
    }
}
