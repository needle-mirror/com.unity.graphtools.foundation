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

        protected override GraphToolState CreateInitialState()
        {
            return new TestGraphToolState(GUID, null);
        }
    }
}
