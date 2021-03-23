using System;
using System.Reflection;
using UnityEditor.Callbacks;
using UnityEditor.VisualScripting.Editor;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model
{
    public static class VisualScriptingIconUtility
    {
        static Texture2D s_GraphAssetModelIcon;

        delegate Texture2D LoadIconRequiredDelegate(string name);

        static LoadIconRequiredDelegate s_LoadIconRequired;

        public static Texture2D LoadIconRequired(string name)
        {
            if (s_LoadIconRequired == null)
                s_LoadIconRequired = (LoadIconRequiredDelegate)
                    typeof(EditorGUIUtility)
                        .GetMethod("LoadIconRequired", BindingFlags.NonPublic | BindingFlags.Static)
                    ?.CreateDelegate(typeof(LoadIconRequiredDelegate));
            return s_LoadIconRequired?.Invoke(name);
        }

        [DidReloadScripts]
        static void OnVisualScriptingIconUtility()
        {
            EditorApplication.projectWindowItemOnGUI = ItemOnGUI;
        }

        static void ItemOnGUI(string guid, Rect rect)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

            Texture2D icon = GetIcon(assetPath);
            if (icon == null)
                return;
            rect.width = rect.height = rect.height - 18;
            rect.y += 2;

            Color col = EditorGUIUtility.isProSkin
                ? new Color(0.22f, 0.22f, 0.22f, 0.0f) // EditorGUIUtility.kDarkViewBackground
                : new Color(0.76f, 0.76f, 0.76f, 1f); // HostView.kViewColor;
            col.a = 1f;
            EditorGUI.DrawRect(rect, col);
            GUI.DrawTexture(rect, icon);
        }

        static Texture2D GetIcon(string assetPath)
        {
            var obj = AssetDatabase.LoadAssetAtPath<VSGraphAssetModel>(assetPath);
            if (obj != null)
            {
                if (s_GraphAssetModelIcon == null)
                    s_GraphAssetModelIcon = VseWindow.GetIcon();
                return s_GraphAssetModelIcon;
            }

            return null;
        }
    }
}
