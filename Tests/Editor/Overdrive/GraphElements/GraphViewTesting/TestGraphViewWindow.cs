using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class TestGraphViewWindow : GraphViewEditorWindow
    {
        public IGraphModel GraphModel { get; private set; }

        public TestGraphViewWindow()
        {
            this.SetDisableInputEvents(true);
            WithSidePanel = false;
        }

        protected override Overdrive.GraphToolState CreateInitialState()
        {
            GraphModel = new GraphModel {StencilType = typeof(TestStencil)};
            return new GraphToolState(GUID, GraphModel);
        }

        protected override GraphView CreateGraphView()
        {
            return new TestGraphView(this, CommandDispatcher);
        }
    }
}
