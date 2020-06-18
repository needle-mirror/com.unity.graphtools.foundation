using System;
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

            const string k_SnappingLineColorRedKey = k_SettingsUniqueKey + "SnappingLineColorRed";
            const string k_SnappingLineColorGreenKey = k_SettingsUniqueKey + "SnappingLineColoGreen";
            const string k_SnappingLineColorBlueKey = k_SettingsUniqueKey + "SnappingLineColoBlue";
            const string k_SnappingLineColorAlphaKey = k_SettingsUniqueKey + "SnappingLineColoAlpha";

            public static readonly Color k_DefaultSnappingLineColor = new Color(68 / 255f, 192 / 255f, 255 / 255f, 68 / 255f);

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
        }

        class Styles
        {
            public static readonly GUIContent kEnableSnapToPortLabel = EditorGUIUtility.TrTextContent("Node Snapping To Port", "If enabled, Nodes align to connected ports.");
            public static readonly GUIContent kEnableSnapToBordersLabel = EditorGUIUtility.TrTextContent("Graph Snapping", "If enabled, GraphElements in Graph Views align with one another when you move them. If disabled, GraphElements move freely.");
            public static readonly GUIContent kSnappingLineColorLabel = new GUIContent("Snapping Line Color", "The color for the graph snapping guidelines");
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
                UserSettings.EnableSnapToPort = !snappingToBorders;
            }

            EditorGUI.BeginChangeCheck();
            var snappingToPort = EditorGUILayout.Toggle(Styles.kEnableSnapToPortLabel, UserSettings.EnableSnapToPort);
            if (EditorGUI.EndChangeCheck())
            {
                UserSettings.EnableSnapToPort = snappingToPort;
                UserSettings.EnableSnapToBorders = !snappingToPort;
            }

            EditorGUI.BeginChangeCheck();
            if (GUILayout.Button("No Snapping"))
            {
                UserSettings.EnableSnapToPort = false;
                UserSettings.EnableSnapToBorders = false;
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
