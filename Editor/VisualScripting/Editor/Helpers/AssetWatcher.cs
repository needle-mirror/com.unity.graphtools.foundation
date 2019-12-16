using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
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

        public void WatchGraphAssetAtPath(string path, GraphAssetModel graphAssetModel)
        {
            if (graphAssetModel != null)
            {
                if (Instance.m_ProjectAssetPaths.ContainsKey(path))
                    Instance.m_ProjectAssetPaths[path] = (graphAssetModel.GraphModel as VSGraphModel)?.SourceFilePath;
                else
                    Instance.m_ProjectAssetPaths.Add(path, (graphAssetModel.GraphModel as VSGraphModel)?.SourceFilePath);
            }
        }

        public void UnwatchGraphAssetAtPath(string path)
        {
            Instance.m_ProjectAssetPaths.Remove(path);
        }

        static AssetWatcher()
        {
            s_Instance = new AssetWatcher();
            Instance.m_ProjectAssetPaths = new Dictionary<string, string>();

            var graphAssetGUIDs = AssetDatabase.FindAssets("t:" + typeof(VSGraphAssetModel).Name);
            foreach (var guid in graphAssetGUIDs)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var graphAssetModel = AssetDatabase.LoadMainAssetAtPath(path) as GraphAssetModel;
                s_Instance.WatchGraphAssetAtPath(path, graphAssetModel);
            }
            // TODO: be smarter
            AssetDatabase.importPackageCompleted += name =>
            {
                EditorReducers.BuildAll(null);
            };
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            var vseWindows = (VseWindow[])Resources.FindObjectsOfTypeAll(typeof(VseWindow));

            if (deletedAssets.Any())
            {
                foreach (var deleted in deletedAssets)
                {
                    if (Instance.m_ProjectAssetPaths.TryGetValue(deleted, out string path))
                    {
                        foreach (var vseWindow in vseWindows)
                            vseWindow.UnloadGraphIfDeleted();

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
                        foreach (var vseWindow in vseWindows)
                            vseWindow.UnloadGraphIfDeleted();

                        // TODO : Fix for 1st drop. Find a better solution
                        var newGraphName = Path.GetFileNameWithoutExtension(newAsset);
                        var oldGraphName = Path.GetFileNameWithoutExtension(oldAsset);

                        // if the Graph has been renamed, not just moved
                        if (!string.IsNullOrEmpty(newGraphName) && newGraphName != oldGraphName)
                        {
                            AssetDatabase.DeleteAsset(path);
                            var newAssetModel = AssetDatabase.LoadAssetAtPath<GraphAssetModel>(newAsset);
                            newAssetModel.name = newGraphName;
                            ((VSGraphModel)newAssetModel.GraphModel).name = newGraphName;
                            foreach (var vseWindow in vseWindows.Where(w => w.CurrentGraphModel == newAssetModel.GraphModel))
                                vseWindow.Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
                        }
                    }
                }
            }

            var importedGraphAssets = importedAssets.Where(AssetAtPathIsGraphAsset).ToList();
            foreach (var importedGraphAsset in importedGraphAssets)
            {
                var path = (AssetDatabase.LoadAssetAtPath<GraphAssetModel>(importedGraphAsset)?.GraphModel as VSGraphModel)?.SourceFilePath;
                if (path != null)
                    Instance.m_ProjectAssetPaths[importedGraphAsset] = path;
                else
                    Instance.m_ProjectAssetPaths.Remove(importedGraphAsset);
            }
            if (importedGraphAssets.Any())
                Version++;
        }

        public static bool AssetAtPathIsGraphAsset(string path)
        {
            if (Path.GetExtension(path) != ".asset")
                return false;

            return typeof(GraphAssetModel).IsAssignableFrom(AssetDatabase.GetMainAssetTypeAtPath(path));
        }
    }

    public class AssetModificationWatcher : AssetModificationProcessor
    {
        public static int Version;

        static string[] OnWillSaveAssets(string[] paths)
        {
            if (paths.Any(p => Path.GetExtension(p) == ".unity"
                && !string.IsNullOrEmpty(Path.GetFileNameWithoutExtension(p))))
            {
                // Build All VS, before returning.
                EditorReducers.BuildAll(null);
            }
            if (paths.Any(AssetWatcher.AssetAtPathIsGraphAsset))
                Version++;

            return paths;
        }

        static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
        {
            if (AssetWatcher.AssetAtPathIsGraphAsset(assetPath))
                Version++;
            return AssetDeleteResult.DidNotDelete;
        }

        static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
        {
            if (AssetWatcher.AssetAtPathIsGraphAsset(sourcePath))
                Version++;
            return AssetMoveResult.DidNotMove;
        }
    }
}
