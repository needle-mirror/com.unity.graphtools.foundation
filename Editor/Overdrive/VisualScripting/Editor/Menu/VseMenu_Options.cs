using System;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.VSPreferences;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    partial class VseMenu
    {
        const string k_ResourcesPath = "Packages/com.unity.graphtools.foundation/Editor/Resources/";

        void CreateOptionsMenu()
        {
            ToolbarButton optionsButton = this.MandatoryQ<ToolbarButton>("optionsButton");
            optionsButton.tooltip = "Options";
            optionsButton.RemoveManipulator(optionsButton.clickable);
            optionsButton.AddManipulator(new Clickable(OnOptionsButton));
        }

        void OnOptionsButton()
        {
            GenericMenu menu = new GenericMenu();
            var graphModel = m_Store.GetState().CurrentGraphModel;
            var vsPreferences = m_Store.GetState().Preferences as VSPreferences;

            void MenuItem(string title, bool value, GenericMenu.MenuFunction onToggle)
                => menu.AddItem(VseUtility.CreatTextContent(title), value, onToggle);

            void MenuToggle(string title, BoolPref k, Action callback = null)
                => MenuItem(title, vsPreferences.GetBool(k), () =>
                {
                    vsPreferences.ToggleBool(k);
                    callback?.Invoke();
                });

            void MenuItemDisable(string title, bool value, GenericMenu.MenuFunction onToggle, Func<bool> shouldDisable)
            {
                if (shouldDisable())
                    menu.AddDisabledItem(VseUtility.CreatTextContent(title));
                else
                    menu.AddItem(VseUtility.CreatTextContent(title), value, onToggle);
            }

            MenuItem("Show Graph in inspector", false, () => Selection.activeObject = graphModel?.AssetModel as Object);
            MenuToggle("Show unused nodes", BoolPref.ShowUnusedNodes, () => m_Store.ForceRefreshUI(UpdateFlags.All));
            MenuItemDisable("Compile", false, () =>
            {
                m_Store.GetState().EditorDataModel.RequestCompilation(RequestCompilationOptions.SaveGraph);
            }, () => (graphModel == null || !graphModel.Stencil.CreateTranslator().SupportsCompilation()));

            menu.AddSeparator("");
            MenuItem("Build All", false, () => m_Store.Dispatch(new BuildAllEditorAction()));
            MenuItem("Migrate all graph assets", false, () =>
            {
                string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(GraphAssetModel)));
                string[] paths = new string[guids.Length];
                for (int i = 0; i < guids.Length; i++)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                    paths[i] = assetPath;
                }

                AssetDatabase.ForceReserializeAssets(paths);
            });

            if (vsPreferences != null)
            {
                MenuItem("Auto-itemize/Variables", vsPreferences.CurrentItemizeOptions.HasFlag(ItemizeOptions.Variables), () =>
                    vsPreferences.ToggleItemizeOption(ItemizeOptions.Variables));
                MenuItem("Auto-itemize/System Constants", vsPreferences.CurrentItemizeOptions.HasFlag(ItemizeOptions.SystemConstants), () =>
                    vsPreferences.ToggleItemizeOption(ItemizeOptions.SystemConstants));
                MenuItem("Auto-itemize/Constants", vsPreferences.CurrentItemizeOptions.HasFlag(ItemizeOptions.Constants), () =>
                    vsPreferences.ToggleItemizeOption(ItemizeOptions.Constants));
            }

            if (Unsupported.IsDeveloperMode())
            {
                MenuItem("Reload Graph", false, () =>
                {
                    if (m_Store.GetState()?.CurrentGraphModel != null)
                    {
                        var path = m_Store.GetState().CurrentGraphModel.GetAssetPath();
                        Selection.activeObject = null;
                        Resources.UnloadAsset((Object)m_Store.GetState().CurrentGraphModel.AssetModel);
                        m_Store.Dispatch(new LoadGraphAssetAction(path));
                    }
                });

                MenuItem("Rebuild UI", false, () =>
                {
                    m_Store.ForceRefreshUI(UpdateFlags.All);
                });
                MenuItem("Rebuild Blackboard", false, () =>
                {
                    m_GraphView.UIController.Blackboard?.Rebuild(GraphElements.Blackboard.RebuildMode.BlackboardOnly);
                });

                menu.AddSeparator("");

                MenuItem("Integrity Check", false, () => graphModel.CheckIntegrity(Verbosity.Verbose));
                MenuItem("Graph cleanup", false, () =>
                {
                    graphModel.QuickCleanup();
                    graphModel.CheckIntegrity(Verbosity.Verbose);
                });
                MenuItem("Fix and reimport all textures", false, OnFixAndReimportTextures);

                MenuToggle("Auto compilation when idle", BoolPref.AutoRecompile);
                MenuToggle("Auto align new dragged edges", BoolPref.AutoAlignDraggedEdges);
                if (Unsupported.IsDeveloperMode())
                {
                    MenuToggle("Dependencies logging", BoolPref.DependenciesLogging);
                    MenuToggle("UI Performance/Always fully rebuild UI on change", BoolPref.FullUIRebuildOnChange);
                    MenuToggle("UI Performance/Warn when UI gets fully rebuilt", BoolPref.WarnOnUIFullRebuild);
                    MenuToggle("UI Performance/Log UI build time", BoolPref.LogUIBuildTime);
                    if (DebugDisplayElement.Allowed)
                        MenuItem("Show Debug", m_GraphView.ShowDebug, () => m_GraphView.ShowDebug = !m_GraphView.ShowDebug);
                    MenuToggle("Diagnostics/Log Recursive Action Dispatch", BoolPref.ErrorOnRecursiveDispatch);
                    MenuToggle("Diagnostics/Log Multiple Actions Dispatch", BoolPref.ErrorOnMultipleDispatchesPerFrame);
                    MenuToggle("Diagnostics/Log All Dispatched Actions", BoolPref.LogAllDispatchedActions);
                }

                foreach (IPluginHandler pluginType in m_Store.GetState().EditorDataModel.PluginRepository.RegisteredPlugins)
                {
                    pluginType.OptionsMenu(menu);
                }
            }

            menu.ShowAsContext();
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
