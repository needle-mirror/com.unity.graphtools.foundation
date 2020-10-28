using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO
{
    public class TestEditorWindow : GraphViewEditorWindow
    {
        public TestEditorWindow()
        {
            this.SetDisableInputEvents(true);
        }

        protected override State CreateInitialState()
        {
            return new TestState(null);
        }
    }
}
