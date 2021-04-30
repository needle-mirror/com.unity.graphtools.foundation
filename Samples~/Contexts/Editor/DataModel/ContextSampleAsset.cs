using System;
using UnityEditor.Callbacks;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts.UI;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts
{
    [Serializable]
    public class ContextSampleAsset : GraphAssetModel
    {
        protected override Type GraphModelType => typeof(ContextSample);

        [MenuItem("Assets/Create/Contexts")]
        public static void CreateGraph(MenuCommand menuCommand)
        {
            const string path = "Assets";
            var template = new GraphTemplate<ContextSampleStencil>(ContextSampleStencil.GraphName);
            CommandDispatcher commandDispatcher = null;
            if (EditorWindow.HasOpenInstances<ContextGraphViewWindow>())
            {
                var window = EditorWindow.GetWindow<ContextGraphViewWindow>();
                if (window != null)
                {
                    commandDispatcher = window.CommandDispatcher;
                }
            }

            GraphAssetCreationHelpers<ContextSampleAsset>.CreateInProjectWindow(template, commandDispatcher, path);
        }

        [OnOpenAsset(1)]
        public static bool OpenGraphAsset(int instanceId, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            if (obj is ContextSampleAsset)
            {
                string path = AssetDatabase.GetAssetPath(instanceId);
                var asset = AssetDatabase.LoadAssetAtPath<ContextSampleAsset>(path);
                if (asset == null)
                    return false;

                var window = GraphViewEditorWindow.FindOrCreateGraphWindow<ContextGraphViewWindow>();
                return window != null;
            }

            return false;
        }
    }
}
