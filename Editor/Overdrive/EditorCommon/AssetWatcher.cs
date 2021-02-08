using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [InitializeOnLoad]
    public class AssetWatcher : AssetPostprocessor
    {
        public static int Version;

        public class Scope : IDisposable
        {
            bool m_PreviousValue;

            public Scope()
            {
                m_PreviousValue = disabled;
                disabled = true;
            }

            public void Dispose()
            {
                disabled = m_PreviousValue;
            }
        }

        public static bool disabled;

        static AssetWatcher s_Instance;
        public static AssetWatcher Instance => s_Instance;

        Dictionary<string, string> m_ProjectAssetPaths;

        static AssetWatcher()
        {
            s_Instance = new AssetWatcher();
        }

        public AssetWatcher()
        {
            m_ProjectAssetPaths = new Dictionary<string, string>();
        }

        public void UnwatchGraphAssetAtPath(string path)
        {
            Instance.m_ProjectAssetPaths.Remove(path);
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            var gvWindows = (GraphViewEditorWindow[])Resources.FindObjectsOfTypeAll(typeof(GraphViewEditorWindow));

            if (deletedAssets.Any())
            {
                foreach (var deleted in deletedAssets)
                {
                    if (Instance.m_ProjectAssetPaths.TryGetValue(deleted, out string path))
                    {
                        foreach (var gvWindow in gvWindows)
                            gvWindow.UnloadGraphIfDeleted();

                        // TODO : Fix for 1st drop. Find a better solution
                        var graphName = Path.GetFileNameWithoutExtension(deleted);
                        if (!string.IsNullOrEmpty(graphName))
                        {
                            AssetDatabase.DeleteAsset(path);
                        }
                    }
                }
            }

            if (movedAssets.Any())
            {
                for (var i = 0; i < movedAssets.Length; ++i)
                {
                    var newAsset = movedAssets[i];
                    var oldAsset = movedFromAssetPaths[i];

                    if (Instance.m_ProjectAssetPaths.TryGetValue(oldAsset, out string path))
                    {
                        foreach (var gvWindow in gvWindows)
                            gvWindow.UnloadGraphIfDeleted();

                        // TODO : Fix for 1st drop. Find a better solution
                        var newGraphName = Path.GetFileNameWithoutExtension(newAsset);
                        var oldGraphName = Path.GetFileNameWithoutExtension(oldAsset);

                        // if the Graph has been renamed, not just moved
                        if (!string.IsNullOrEmpty(newGraphName) && newGraphName != oldGraphName)
                        {
                            AssetDatabase.DeleteAsset(path);
                            if (AssetDatabase.LoadAssetAtPath<Object>(newAsset) is IGraphAssetModel newAssetModel)
                            {
                                newAssetModel.Name = newGraphName;
                                newAssetModel.GraphModel.Name = newGraphName;
                                foreach (var gvWindow in gvWindows.Where(w => w.CommandDispatcher.GraphToolState?.GraphModel == newAssetModel.GraphModel))
                                {
                                    gvWindow.CommandDispatcher.MarkStateDirty();
                                    gvWindow.ProcessGraph();
                                }
                            }
                        }
                    }
                }
            }

            var importedGraphAssets = importedAssets.Where(AssetAtPathIsGraphAsset).ToList();
            if (importedGraphAssets.Any())
                Version++;
        }

        public static bool AssetAtPathIsGraphAsset(string path)
        {
            if (Path.GetExtension(path) != ".asset")
                return false;

            return typeof(IGraphAssetModel).IsAssignableFrom(AssetDatabase.GetMainAssetTypeAtPath(path));
        }
    }
}
