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
            return new TestState(GUID, null);
        }

        protected override BlankPage CreateBlankPage()
        {
            return null;
        }

        protected override MainToolbar CreateMainToolbar()
        {
            return null;
        }

        protected override ErrorToolbar CreateErrorToolbar()
        {
            return null;
        }

        protected override GtfoGraphView CreateGraphView()
        {
            return null;
        }
    }
}
