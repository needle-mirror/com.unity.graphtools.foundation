using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    public class GraphTemplate<TStencil> : IGraphTemplate where TStencil : Stencil
    {
        public GraphTemplate(string graphName = "Graph")
        {
            GraphTypeName = graphName;
        }

        public virtual Type StencilType => typeof(TStencil);

        public virtual void InitBasicGraph(IGraphModel graphModel)
        {
        }

        public virtual string GraphTypeName { get; }

        public virtual string DefaultAssetName => GraphTypeName;
    }
}
