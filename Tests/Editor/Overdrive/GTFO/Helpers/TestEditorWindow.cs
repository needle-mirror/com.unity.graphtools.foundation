using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.Helpers
{
    public class TestEditorWindow : EditorWindow
    {
        public TestEditorWindow()
        {
            this.SetDisableInputEvents(true);
        }
    }
}
