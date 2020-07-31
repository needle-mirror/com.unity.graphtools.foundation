using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class GraphViewSettings
    {
        internal class UserSettings
        {
            const string k_SettingsUniqueKey = "UnityEditor.Graph/";

            const string k_EnableSnapToPortKey = k_SettingsUniqueKey + "GraphEditorSetting.enableSnapToPort";
            const string k_EnableSnapToBordersKey = k_SettingsUniqueKey + "GraphEditorSetting.enableSnapToBorders";
            const string k_EnableSnapToGridKey = k_SettingsUniqueKey + "GraphEditorSetting.enableSnapToGrid";
            const string k_EnableSnapToSpacingKey = k_SettingsUniqueKey + "GraphEditorSetting.enableSnapToSpacing";

            const string k_SnappingLineColorRedKey = k_SettingsUniqueKey + "SnappingLineColorRed";
            const string k_SnappingLineColorGreenKey = k_SettingsUniqueKey + "SnappingLineColoGreen";
            const string k_SnappingLineColorBlueKey = k_SettingsUniqueKey + "SnappingLineColoBlue";
            const string k_SnappingLineColorAlphaKey = k_SettingsUniqueKey + "SnappingLineColoAlpha";

            public static readonly Color k_DefaultSnappingLineColor = new Color(68 / 255f, 192 / 255f, 255 / 255f, 68 / 255f);

            static Dictionary<Type, bool> s_SnappingStrategiesStates = new Dictionary<Type, bool>()
            {
                {typeof(SnapToBordersStrategy), EnableSnapToBorders},
                {typeof(SnapToPortStrategy), EnableSnapToPort},
                {typeof(SnapToGridStrategy), EnableSnapToGrid},
                {typeof(SnapToSpacingStrategy), EnableSnapToSpacing}
            };

            public static Color SnappingLineColor
            {
                get =>
                    new Color
                {
                    r = EditorPrefs.GetFloat(k_SnappingLineColorRedKey, k_DefaultSnappingLineColor.r),
                    g = EditorPrefs.GetFloat(k_SnappingLineColorGreenKey, k_DefaultSnappingLineColor.g),
                    b = EditorPrefs.GetFloat(k_SnappingLineColorBlueKey, k_DefaultSnappingLineColor.b),
                    a = EditorPrefs.GetFloat(k_SnappingLineColorAlphaKey, k_DefaultSnappingLineColor.a)
                };
                set
                {
                    EditorPrefs.SetFloat(k_SnappingLineColorRedKey, value.r);
                    EditorPrefs.SetFloat(k_SnappingLineColorGreenKey, value.g);
                    EditorPrefs.SetFloat(k_SnappingLineColorBlueKey, value.b);
                    EditorPrefs.SetFloat(k_SnappingLineColorAlphaKey, value.a);
                }
            }

            public static bool EnableSnapToPort
            {
                get => EditorPrefs.GetBool(k_EnableSnapToPortKey, false);
                set => EditorPrefs.SetBool(k_EnableSnapToPortKey, value);
            }

            public static bool EnableSnapToBorders
            {
                get => EditorPrefs.GetBool(k_EnableSnapToBordersKey, false);
                set => EditorPrefs.SetBool(k_EnableSnapToBordersKey, value);
            }

            public static bool EnableSnapToGrid
            {
                get => EditorPrefs.GetBool(k_EnableSnapToGridKey, false);
                set => EditorPrefs.SetBool(k_EnableSnapToGridKey, value);
            }

            public static bool EnableSnapToSpacing
            {
                get => EditorPrefs.GetBool(k_EnableSnapToSpacingKey, false);
                set => EditorPrefs.SetBool(k_EnableSnapToSpacingKey, value);
            }

            public static Dictionary<Type, bool> SnappingStrategiesStates
            {
                get
                {
                    UpdateSnappingStates();
                    return s_SnappingStrategiesStates;
                }
            }

            static void UpdateSnappingStates()
            {
                s_SnappingStrategiesStates[typeof(SnapToBordersStrategy)] = EnableSnapToBorders;
                s_SnappingStrategiesStates[typeof(SnapToPortStrategy)] = EnableSnapToPort;
                s_SnappingStrategiesStates[typeof(SnapToGridStrategy)] = EnableSnapToGrid;
                s_SnappingStrategiesStates[typeof(SnapToSpacingStrategy)] = EnableSnapToSpacing;
            }
        }

        class Styles
        {
            public static readonly GUIContent kEnableSnapToPortLabel = EditorGUIUtility.TrTextContent("Node Port Snapping", "If enabled, Nodes align to connected ports.");
            public static readonly GUIContent kEnableSnapToBordersLabel = EditorGUIUtility.TrTextContent("Graph Snapping", "If enabled, GraphElements in Graph Views align with one another when you move them. If disabled, GraphElements move freely.");
            public static readonly GUIContent kEnableSnapToGridLabel = EditorGUIUtility.TrTextContent("Grid Snapping", "If enabled, GraphElements in Graph Views align with the grid.");
            public static readonly GUIContent kEnableSnapToSpacingLabel = EditorGUIUtility.TrTextContent("Spacing Snapping", "If enabled, GraphElements align to keep equal spacing with their neighbors.");
            public static readonly GUIContent kSnappingLineColorLabel = new GUIContent("Snapping Line Color", "The color for the snapping guidelines");
        }

        [SettingsProvider]
        static SettingsProvider CreateProjectSettingsProvider()
        {
            var provider = new SettingsProvider("Preferences/Graph", SettingsScope.User, SettingsProvider.GetSearchKeywordsFromGUIContentProperties<Styles>());
            provider.guiHandler = searchContext => OnGUI();
            return provider;
        }

        static void OnGUI()
        {
            // For the moment, the different types of snapping can only be used separately
            EditorGUI.BeginChangeCheck();
            var snappingToBorders = EditorGUILayout.Toggle(Styles.kEnableSnapToBordersLabel, UserSettings.EnableSnapToBorders);
            if (EditorGUI.EndChangeCheck())
            {
                UserSettings.EnableSnapToBorders = snappingToBorders;
            }

            EditorGUI.BeginChangeCheck();
            var snappingToPort = EditorGUILayout.Toggle(Styles.kEnableSnapToPortLabel, UserSettings.EnableSnapToPort);
            if (EditorGUI.EndChangeCheck())
            {
                UserSettings.EnableSnapToPort = snappingToPort;
            }

            EditorGUI.BeginChangeCheck();
            var snappingToGrid = EditorGUILayout.Toggle(Styles.kEnableSnapToGridLabel, UserSettings.EnableSnapToGrid);
            if (EditorGUI.EndChangeCheck())
            {
                UserSettings.EnableSnapToGrid = snappingToGrid;
            }

            EditorGUI.BeginChangeCheck();
            var snappingToSpacing = EditorGUILayout.Toggle(Styles.kEnableSnapToSpacingLabel, UserSettings.EnableSnapToSpacing);
            if (EditorGUI.EndChangeCheck())
            {
                UserSettings.EnableSnapToSpacing = snappingToSpacing;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            var newSnappingLineColor = EditorGUILayout.ColorField(Styles.kSnappingLineColorLabel, UserSettings.SnappingLineColor);

            if (EditorGUI.EndChangeCheck())
            {
                UserSettings.SnappingLineColor = newSnappingLineColor;
                InternalEditorUtility.RepaintAllViews();
            }

            if (GUILayout.Button("Reset"))
            {
                UserSettings.SnappingLineColor = UserSettings.k_DefaultSnappingLineColor;
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
