using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive.VisualScripting;
using Object = UnityEngine.Object;

#if !ENABLE_VSTU
namespace SyntaxTree {public static class ThisNamespaceOnlyExistsBecauseVisualStudioIntegrationWillHijackItAndForceFullyQualifiedNamesForRoslynsSyntaxTreeType{}}
#endif

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public static class VseUtility
    {
        public static string GetUniqueAssetPathNameInActiveFolder(string filename)
        {
            string path;
            try
            {
                // Private implementation of a file naming function which puts the file at the selected path.
                var assetDatabase = typeof(AssetDatabase);
                path = (string)assetDatabase.GetMethod("GetUniquePathNameAtSelectedPath", BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(assetDatabase, new object[] { filename });
            }
            catch
            {
                // Protection against implementation changes.
                path = AssetDatabase.GenerateUniqueAssetPath("Assets/" + filename);
            }
            return path;
        }

        public static GUIContent CreatTextContent(string content)
        {
            // TODO: Replace by EditorGUIUtility.TrTextContent when it's made 'public'.
            return new GUIContent(content);
        }

        public static void LogSticky(LogType logType, LogOption logOptions, string message, string file, int instanceId)
        {
            ConsoleWindowBridge.LogSticky(message, file, logType, logOptions, instanceId);
        }

        public static void RemoveLogEntries()
        {
            ConsoleWindowBridge.RemoveLogEntries();
        }

        public static void SetupLogStickyCallback()
        {
            ConsoleWindowBridge.SetEntryDoubleClickedDelegate((file, entryInstanceId) =>
            {
                string[] pathAndGuid = file.Split('@');
                VseWindow window = VseWindow.OpenVseAssetInWindow(pathAndGuid[0]);
                if (GUID.TryParse(pathAndGuid[1], out GUID guid))
                    window.Store?.Dispatch(new PanToNodeAction(guid));
            });
        }
    }
}
