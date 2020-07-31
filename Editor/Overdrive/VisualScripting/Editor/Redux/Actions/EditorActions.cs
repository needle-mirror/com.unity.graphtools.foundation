using System;
using UnityEditor.Compilation;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class BuildAllEditorAction : IAction
    {
        public readonly Action<string, CompilerMessage[]> Callback;

        public BuildAllEditorAction(Action<string, CompilerMessage[]> callback = null)
        {
            Callback = callback;
        }
    }
}
