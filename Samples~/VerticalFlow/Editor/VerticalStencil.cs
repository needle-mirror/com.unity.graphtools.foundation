using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Vertical
{
    class VerticalStencil : Stencil
    {
        public static string toolName = "Vertical Flow Editor";

        public override string ToolName => toolName;

        public override IGraphProcessingErrorModel CreateProcessingErrorModel(GraphProcessingError error)
        {
            if (error.SourceNode != null && !error.SourceNode.Destroyed)
            {
                return new GraphProcessingErrorModel(error);
            }

            return null;
        }

        /// <inheritdoc />
        public override IBlackboardGraphModel CreateBlackboardGraphModel(IGraphAssetModel graphAssetModel)
        {
            return new VerticalBlackboardGraphModel(graphAssetModel);
        }

        public static readonly string k_GraphName = "VerticalFlow";
    }
}
