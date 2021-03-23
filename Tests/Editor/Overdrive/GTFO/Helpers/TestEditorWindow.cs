using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO
{
    public class TestEditorWindow : GraphViewEditorWindow
    {
        public TestEditorWindow()
        {
            this.SetDisableInputEvents(true);
        }

        public IGraphModel GraphModel { get; private set; }

        protected override GraphToolState CreateInitialState()
        {
            GraphModel = new GraphModel();
            var state = new TestGraphToolState(GUID, GraphModel);
            using (var graphUpdater = state.GraphViewState.Updater)
            {
                graphUpdater.U.Position = Vector3.zero;
                graphUpdater.U.Scale = Vector3.one;
            }
            return state;
        }
    }
}
