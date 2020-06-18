using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Plugins;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.Editor", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public struct OpenedGraph
    {
        public GraphAssetModel GraphAssetModel;
        public GameObject BoundObject;

        public OpenedGraph(GraphAssetModel graphAssetModel, GameObject boundObject)
        {
            GraphAssetModel = graphAssetModel;
            BoundObject = boundObject;
        }
    }
    public interface IEditorDataModel : IGTFEditorDataModel
    {
        IGraphElementModel ElementModelToRename { get; set; }
        GUID NodeToFrameGuid { get; set; }
        int CurrentGraphIndex { get; }
        VSPreferences Preferences { get; }
        GameObject BoundObject { get; set; }
        IPluginRepository PluginRepository { get; }
        List<OpenedGraph> PreviousGraphModels { get; }
        int UpdateCounter { get; set; }
        bool TracingEnabled { get; set; }
        bool CompilationPending { get; set; }

        void RequestCompilation(RequestCompilationOptions options);

        bool ShouldSelectElementUponCreation(IHasGraphElementModel hasGraphElementModel);

        void SelectElementsUponCreation(IEnumerable<IGraphElementModel> graphElementModels, bool select);

        void ClearElementsToSelectUponCreation();

        bool ShouldExpandBlackboardRowUponCreation(string rowName);

        void ExpandBlackboardRowsUponCreation(IEnumerable<string> rowNames, bool expand);

        bool ShouldExpandElementUponCreation(IVisualScriptingField visualScriptingField);

        void ExpandElementsUponCreation(IEnumerable<IVisualScriptingField> visualScriptingFields, bool expand);
    }

    public enum RequestCompilationOptions
    {
        Default,
        SaveGraph,
    }
}
