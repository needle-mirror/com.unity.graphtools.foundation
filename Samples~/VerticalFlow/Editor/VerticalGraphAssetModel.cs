using System;
using UnityEditor.Callbacks;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Vertical
{
    class VerticalGraphAssetModel : GraphAssetModel
    {
        [MenuItem("Assets/Create/VerticalFlow")]
        public static void CreateGraph(MenuCommand menuCommand)
        {
            const string path = "Assets";
            var template = new GraphTemplate<VerticalStencil>(VerticalStencil.k_GraphName);
            CommandDispatcher commandDispatcher = null;
            if (EditorWindow.HasOpenInstances<VerticalGraphWindow>())
            {
                var window = EditorWindow.GetWindow<VerticalGraphWindow>();
                if (window != null)
                {
                    commandDispatcher = window.CommandDispatcher;
                }
            }

            GraphAssetCreationHelpers<VerticalGraphAssetModel>.CreateInProjectWindow(template, commandDispatcher, path);
        }

        [OnOpenAsset(1)]
        public static bool OpenGraphAsset(int instanceId, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            if (obj is VerticalGraphAssetModel)
            {
                string path = AssetDatabase.GetAssetPath(instanceId);
                var asset = AssetDatabase.LoadAssetAtPath<VerticalGraphAssetModel>(path);
                if (asset == null)
                    return false;

                var window = GraphViewEditorWindow.FindOrCreateGraphWindow<VerticalGraphWindow>();
                return window != null;
            }

            return false;
        }

        protected override Type GraphModelType => typeof(VerticalGraphModel);
    }
}
