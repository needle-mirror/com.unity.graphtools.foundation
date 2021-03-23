using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.UIElements;
using UnityEditor.VisualScripting.Editor.Plugins;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEditor.VisualScripting.Model.Translators;
using UnityEngine;
using UnityEngine.UIElements;
using VisualScripting.Editor.Elements;
using static UnityEditor.VisualScripting.Model.VSPreferences;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Editor
{
    partial class VseMenu
    {
        const string k_VisualScriptingLogCompileTimeStats = "VisualScripting.LogCompileTimeStat";
        const string k_ResourcesPath = "Packages/com.unity.graphtools.foundation/Editor/Resources/";

        void CreateOptionsMenu()
        {
            ToolbarButton optionsButton = this.MandatoryQ<ToolbarButton>("optionsButton");
            optionsButton.tooltip = "Options";
            optionsButton.RemoveManipulator(optionsButton.clickable);
            optionsButton.AddManipulator(new Clickable(OnOptionsButton));
            RoslynTranslator.LogCompileTimeStats = EditorPrefs.GetBool(k_VisualScriptingLogCompileTimeStats, false);
        }

        void OnOptionsButton()
        {
            GenericMenu menu = new GenericMenu();
            VSGraphModel vsGraphModel = (VSGraphModel)m_Store.GetState().CurrentGraphModel;
            VSPreferences pref = m_Store.GetState().Preferences;

            void MenuItem(string title, bool value, GenericMenu.MenuFunction onToggle)
                => menu.AddItem(VseUtility.CreatTextContent(title), value, onToggle);

            void MenuToggle(string title, BoolPref k, Action callback = null)
                => MenuItem(title, pref.GetBool(k), () =>
                {
                    pref.ToggleBool(k);
                    callback?.Invoke();
                });

            void MenuMapToggle(string title, Func<bool> match, GenericMenu.MenuFunction onToggle)
                => MenuItem(title, match(), onToggle);

            void MenuItemDisable(string title, bool value, GenericMenu.MenuFunction onToggle, Func<bool> shouldDisable)
            {
                if (shouldDisable())
                    menu.AddDisabledItem(VseUtility.CreatTextContent(title));
                else
                    menu.AddItem(VseUtility.CreatTextContent(title), value, onToggle);
            }

            MenuItem("Build All", false, AssetDatabase.SaveAssets);
            MenuItemDisable("Compile", false, () =>
            {
                m_Store.GetState().EditorDataModel.RequestCompilation(RequestCompilationOptions.SaveGraph);
            }, () => (vsGraphModel == null || !vsGraphModel.Stencil.CreateTranslator().SupportsCompilation()));
            MenuItem("Auto-itemize/Variables", pref.CurrentItemizeOptions.HasFlag(ItemizeOptions.Variables), () =>
                pref.ToggleItemizeOption(ItemizeOptions.Variables));
            MenuItem("Auto-itemize/System Constants", pref.CurrentItemizeOptions.HasFlag(ItemizeOptions.SystemConstants), () =>
                pref.ToggleItemizeOption(ItemizeOptions.SystemConstants));
            MenuItem("Auto-itemize/Constants", pref.CurrentItemizeOptions.HasFlag(ItemizeOptions.Constants), () =>
                pref.ToggleItemizeOption(ItemizeOptions.Constants));
            MenuToggle("Show unused nodes", BoolPref.ShowUnusedNodes, () => m_Store.Dispatch(new RefreshUIAction(UpdateFlags.All)));
            if (Unsupported.IsDeveloperMode())
            {
                MenuItem("Log compile time stats", LogCompileTimeStats, () => LogCompileTimeStats = !LogCompileTimeStats);

                MenuItem("Rebuild UI", false, () =>
                {
                    m_Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
                });
                MenuItem("Rebuild Blackboard", false, () =>
                {
                    m_GraphView.UIController.Blackboard?.Rebuild(Blackboard.RebuildMode.BlackboardOnly);
                });
                menu.AddSeparator("");
                MenuItem("Reload and Rebuild UI", false, () =>
                {
                    if (m_Store.GetState()?.CurrentGraphModel != null)
                    {
                        var path = m_Store.GetState().CurrentGraphModel.GetAssetPath();
                        Selection.activeObject = null;
                        Resources.UnloadAsset((Object)m_Store.GetState().CurrentGraphModel.AssetModel);
                        m_Store.Dispatch(new LoadGraphAssetAction(path));
                    }
                });

                MenuItem("Layout", false, () =>
                {
                    m_GraphView.FrameAll();
                    m_Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
                });

                menu.AddSeparator("");
                MenuItem("Clear Searcher Databases", false, () =>
                {
                    var provider = m_Store.GetState().CurrentGraphModel.Stencil.GetSearcherDatabaseProvider();
                    provider.ClearTypesItemsSearcherDatabases();
                    provider.ClearTypeMembersSearcherDatabases();
                    provider.ClearGraphElementsSearcherDatabases();
                    provider.ClearGraphVariablesSearcherDatabases();
                    provider.ClearReferenceItemsSearcherDatabases();
                });
                MenuItem("Integrity Check", false, () => vsGraphModel.CheckIntegrity(GraphModel.Verbosity.Verbose));
                MenuItem("Graph cleanup", false, () =>
                {
                    vsGraphModel.QuickCleanup();
                    vsGraphModel.CheckIntegrity(GraphModel.Verbosity.Verbose);
                });
                MenuItem("Fix and reimport all textures", false, OnFixAndReimportTextures);

                MenuToggle("Auto compilation when idle", BoolPref.AutoRecompile);
                MenuToggle("Auto align new dragged edges", BoolPref.AutoAlignDraggedEdges);
                if (Unsupported.IsDeveloperMode())
                {
                    MenuToggle("Bound object logging", BoolPref.BoundObjectLogging);
                    MenuToggle("Dependencies logging", BoolPref.DependenciesLogging);
                    MenuToggle("UI Performance/Always fully rebuild UI on change", BoolPref.FullUIRebuildOnChange);
                    MenuToggle("UI Performance/Warn when UI gets fully rebuilt", BoolPref.WarnOnUIFullRebuild);
                    MenuToggle("UI Performance/Log UI build time", BoolPref.LogUIBuildTime);
                    if (DebugDisplayElement.Allowed)
                        MenuItem("Show Debug", m_GraphView.ShowDebug, () => m_GraphView.ShowDebug = !m_GraphView.ShowDebug);
                    MenuToggle("Diagnostics/Log Recursive Action Dispatch", BoolPref.ErrorOnRecursiveDispatch);
                    MenuToggle("Diagnostics/Log Multiple Actions Dispatch", BoolPref.ErrorOnMultipleDispatchesPerFrame);
                    MenuToggle("Diagnostics/Log All Dispatched Actions", BoolPref.LogAllDispatchedActions);
                    MenuItem("Spawn all node types in graph", false, () =>
                    {
                        VSGraphModel graph = (VSGraphModel)m_Store.GetState().CurrentGraphModel;
                        Stencil stencil = graph.Stencil;
                        Vector2 nextPosition = Vector2.zero;
                        Vector2 spaceBetween = new Vector2(300, 0);
                        foreach (var node in stencil.SpawnAllNodes(graph))
                        {
                            node.Position += nextPosition;
                            nextPosition += spaceBetween;
                        }
                    });
                }

                foreach (IPluginHandler pluginType in m_Store.GetState().EditorDataModel.PluginRepository.RegisteredPlugins)
                {
                    pluginType.OptionsMenu(menu);
                }

                var compilationResult = m_Store.GetState()?.CompilationResultModel?.GetLastResult();
                if (compilationResult?.pluginSourceCode != null)
                {
                    foreach (var pluginType in compilationResult.pluginSourceCode.Keys)
                    {
                        MenuMapToggle(title: "CodeViewer/Plugin/" + pluginType.Name, match: () => pref.PluginTypePref == pluginType, onToggle: () =>
                        {
                            VseUtility.UpdateCodeViewer(show: true, pluginIndex: pluginType,
                                compilationResult: compilationResult,
                                selectionDelegate: lineMetadata =>
                                {
                                    if (lineMetadata == null)
                                        return;

                                    GUID nodeGuid = (GUID)lineMetadata;
                                    m_Store.Dispatch(new PanToNodeAction(nodeGuid));
                                });
                            pref.PluginTypePref = pluginType;
                        });
                    }
                }
            }

            menu.ShowAsContext();
        }

        static bool LogCompileTimeStats
        {
            get => RoslynTranslator.LogCompileTimeStats;
            set
            {
                EditorPrefs.SetBool(k_VisualScriptingLogCompileTimeStats, value);
                RoslynTranslator.LogCompileTimeStats = value;
            }
        }

        static void OnFixAndReimportTextures()
        {
            foreach (var assetPath in AssetDatabase.GetAllAssetPaths().Where(x => x.Contains(k_ResourcesPath)))
            {
                TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (textureImporter == null)
                    continue;

                // Default settings for all editor resources images
                TextureImporterNPOTScale npotScale = TextureImporterNPOTScale.None;
                TextureImporterType textureType = TextureImporterType.Default;
                TextureImporterShape textureShape = TextureImporterShape.Texture2D;
                TextureImporterCompression textureCompression = TextureImporterCompression.Uncompressed;
                TextureWrapMode wrapMode = TextureWrapMode.Clamp;
                FilterMode filterMode = FilterMode.Bilinear;
                bool mipmapEnabled = false;
                bool srgbTexture = true;
                bool isReadable = false;
                float mipMapBias = 0;
                bool? alphaIsTransparency = null;

                // Overwrite settings for brushes
                if (assetPath.Contains("Brushes/"))
                {
                    textureType = TextureImporterType.SingleChannel;
                    isReadable = true;
                }

                // Overwrite settings for cursors
                if (assetPath.Contains("Cursors/"))
                {
                    textureType = TextureImporterType.Cursor;
                    isReadable = true;
                    alphaIsTransparency = true;
                    filterMode = FilterMode.Point;
                }

                // Make skin images point sampled for nicer retina support
                if (assetPath.ToLower().Contains("builtin skins/") || assetPath.ToLower().Contains("/icons/"))
                {
                    filterMode = FilterMode.Point;
                }

                // Overwrite settings for gizmo icons
                if (assetPath.ToLower().Contains("gizmo") && !assetPath.ToLower().Contains("builtin skins/"))
                {
                    filterMode = FilterMode.Bilinear;
                    mipMapBias = -0.5f;
                }

                if (assetPath.Contains("MipLevels"))
                {
                    isReadable = true;
                }

                if (assetPath.Contains("_MIP"))
                {
                    alphaIsTransparency = true;
                }

                if (assetPath.Contains("GraphView") || assetPath.ToLower().Contains("visualscripting"))
                {
                    textureType = TextureImporterType.GUI;
                    filterMode = FilterMode.Point;
                }

                // Important that the settings are only set here at the end.
                // Otherwise they might be set first to a different value and then back to the old value.
                // This would dirty the meta files even if the end result was the same as before.
                if (textureImporter.npotScale != npotScale)
                    textureImporter.npotScale = npotScale;
                if (textureImporter.textureType != textureType)
                    textureImporter.textureType = textureType;
                if (textureImporter.textureShape != textureShape)
                    textureImporter.textureShape = textureShape;
                if (textureImporter.textureCompression != textureCompression)
                    textureImporter.textureCompression = textureCompression;
                if (textureImporter.wrapMode != wrapMode)
                    textureImporter.wrapMode = wrapMode;
                if (textureImporter.filterMode != filterMode)
                    textureImporter.filterMode = filterMode;
                if (textureImporter.mipmapEnabled != mipmapEnabled)
                    textureImporter.mipmapEnabled = mipmapEnabled;
                if (textureImporter.sRGBTexture != srgbTexture)
                    textureImporter.sRGBTexture = srgbTexture;
                if (textureImporter.isReadable != isReadable)
                    textureImporter.isReadable = isReadable;
                if (Math.Abs(textureImporter.mipMapBias - mipMapBias) > Mathf.Epsilon)
                    textureImporter.mipMapBias = mipMapBias;
                if (alphaIsTransparency.HasValue && textureImporter.alphaIsTransparency != alphaIsTransparency.Value)
                    textureImporter.alphaIsTransparency = alphaIsTransparency.Value;

                textureImporter.SaveAndReimport();
            }
        }
    }
}
