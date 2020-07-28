using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements.Utilities
{
    public class TestGraphViewWindow : EditorWindow
    {
        public GraphView GraphView { get; private set; }
        public Store Store { get; private set; }
        public BasicGraphModel GraphModel { get; private set; }

        public TestGraphViewWindow()
        {
            this.SetDisableInputEvents(true);
        }

        public void OnEnable()
        {
            GraphModel = new BasicGraphModel();
            Store = new Store(new State(GraphModel), StoreHelper.RegisterReducers);
            GraphView = new TestGraphView(Store);

            GraphView.name = "theView";
            GraphView.viewDataKey = "theView";
            GraphView.StretchToParentSize();

            rootVisualElement.Add(GraphView);
        }

        public void OnDisable()
        {
            rootVisualElement.Remove(GraphView);
        }
    }
}
