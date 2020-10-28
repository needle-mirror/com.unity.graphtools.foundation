using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class TestGraphViewWindow : GraphViewEditorWindow
    {
        public IGraphModel GraphModel { get; private set; }

        public TestGraphViewWindow()
        {
            this.SetDisableInputEvents(true);
        }

        protected override Overdrive.State CreateInitialState()
        {
            GraphModel = new GraphModel();
            ((GraphModel)GraphModel).Stencil = new TestStencil();
            return new State(GraphModel);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_GraphView = new TestGraphView(this, Store);
            rootVisualElement.Add(GraphView);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            rootVisualElement.Remove(GraphView);
        }
    }
}
