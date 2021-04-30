using System.Collections.Generic;
using System.Linq;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// A model that represents a blackboard for a graph.
    /// </summary>
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class BlackboardGraphModel : GraphElementModel, IBlackboardGraphModel
    {
        public bool Valid => GraphModel != null;

        /// <summary>
        /// Initializes a new instance of the BlackboardGraphModel class.
        /// </summary>
        /// <param name="graphAssetModel">The graph asset model used as the data source.</param>
        public BlackboardGraphModel(IGraphAssetModel graphAssetModel)
        {
            AssetModel = graphAssetModel;
        }

        public virtual string GetBlackboardTitle()
        {
            return AssetModel?.FriendlyScriptName ?? "";
        }

        public virtual string GetBlackboardSubTitle()
        {
            return "Class Library";
        }

        public virtual IEnumerable<string> SectionNames =>
            GraphModel == null ? Enumerable.Empty<string>() : new List<string>() { "Graph Variables" };

        public virtual IEnumerable<IVariableDeclarationModel> GetSectionRows(string sectionName)
        {
            return GraphModel?.VariableDeclarations ?? Enumerable.Empty<IVariableDeclarationModel>();
        }
    }
}
