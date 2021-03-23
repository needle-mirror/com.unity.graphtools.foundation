using System;
using UnityEditor.Callbacks;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    public static class VSBuildPostProcessor
    {
        [PostProcessBuild(1)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            EditorReducers.BuildAll(null);
        }
    }
}
