using System;
using UnityEditor.EditorCommon.Redux;
using UnityEditor.Compilation;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    public class BuildAllEditorAction : IAction
    {
        public readonly Action<string, CompilerMessage[]> Callback;

        public BuildAllEditorAction(Action<string, CompilerMessage[]> callback = null)
        {
            Callback = callback;
        }
    }

    public class AddVisualScriptToObjectAction : IAction
    {
        public readonly string AssetPath;
        public readonly UnityEngine.Object Instance;
        public readonly Type ComponentType;

        public AddVisualScriptToObjectAction(string assetPath, Type componentType, UnityEngine.Object instance = null)
        {
            AssetPath = assetPath;
            Instance = instance;
            ComponentType = componentType;
        }
    }
}
