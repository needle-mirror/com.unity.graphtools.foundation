using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEngine;
using Object = UnityEngine.Object;

#if !ENABLE_VSTU
namespace SyntaxTree {public static class ThisNamespaceOnlyExistsBecauseVisualStudioIntegrationWillHijackItAndForceFullyQualifiedNamesForRoslynsSyntaxTreeType{}}
#endif

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public static class VseUtility
    {
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
    }
}
