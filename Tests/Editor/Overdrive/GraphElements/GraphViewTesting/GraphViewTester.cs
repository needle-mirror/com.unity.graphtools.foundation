using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.Stylesheets;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements.Utilities
{
    public class State : GraphToolsFoundation.Overdrive.GraphElements.State
    {
        public override IGTFGraphModel GraphModel { get; }

        public State(BasicGraphModel graphModel)
        {
            GraphModel = graphModel;
        }
    }

    public class TestStore : Overdrive.GraphElements.Store<State>
    {
        public TestStore(State initialState)
            : base(initialState)
        {
        }
    }

    public class GraphViewTester
    {
        static readonly Rect k_WindowRect = new Rect(Vector2.zero, new Vector2(SelectionDragger.k_PanAreaWidth * 8, SelectionDragger.k_PanAreaWidth * 6));

        protected TestGraphViewWindow window { get; private set; }
        protected TestGraphView graphView { get; private set; }
        protected TestEventHelpers helpers { get; private set; }
        protected BasicGraphModel GraphModel => window.GraphModel;
        protected TestStore Store => window.Store;

        bool m_EnablePersistence;

        public GraphViewTester(bool enablePersistence = false)
        {
            m_EnablePersistence = enablePersistence;
        }

        bool m_SavedUseNewStylesheets;
        [SetUp]
        public virtual void SetUp()
        {
            m_SavedUseNewStylesheets = GraphElementsHelper.UseNewStylesheets;
            GraphElementsHelper.UseNewStylesheets = true;

            window = EditorWindow.GetWindowWithRect<TestGraphViewWindow>(k_WindowRect);

            if (!m_EnablePersistence)
                window.DisableViewDataPersistence();
            else
                window.ClearPersistentViewData();

            graphView = window.GraphView as TestGraphView;
            StylesheetsHelper.AddTestStylesheet(graphView, "Tests.uss");

            helpers = new TestEventHelpers(window);
        }

        [TearDown]
        public virtual void TearDown()
        {
            GraphElementsHelper.UseNewStylesheets = m_SavedUseNewStylesheets;
            GraphElementFactory.RemoveAll(graphView);

            if (m_EnablePersistence)
                window.ClearPersistentViewData();

            Clear();
        }

        protected void Clear()
        {
            // See case: https://fogbugz.unity3d.com/f/cases/998343/
            // Clearing the capture needs to happen before closing the window
            MouseCaptureController.ReleaseMouse();
            if (window != null)
            {
                window.Close();
            }
        }

        protected BasicNodeModel CreateNode(string title = "", Vector2 position = default, int inCount = 0, int outCount = 0, Orientation orientation = Orientation.Horizontal)
        {
            return CreateNode<BasicNodeModel>(title, position, inCount, outCount, orientation);
        }

        protected TNodeModel CreateNode<TNodeModel>(string title, Vector2 position, int inCount = 0, int outCount = 0, Orientation orientation = Orientation.Horizontal) where TNodeModel : BasicNodeModel, new()
        {
            var node = GraphModel.CreateNode<TNodeModel>(title);
            node.Position = position;

            for (int i = 0; i < outCount; i++)
            {
                node.AddPort(orientation, Direction.Output, PortCapacity.Multi, typeof(float));
            }
            for (int i = 0; i < inCount; i++)
            {
                node.AddPort(orientation, Direction.Input, PortCapacity.Single, typeof(float));
            }

            return node;
        }

        protected IEnumerator ConnectPorts(IGTFPortModel fromPort, IGTFPortModel toPort)
        {
            var fromPortUI = fromPort.GetUI<Port>(graphView);
            var toPortUI = toPort.GetUI<Port>(graphView);

            Assert.IsNotNull(fromPortUI);
            Assert.IsNotNull(toPortUI);

            // Drag an edge between the two ports
            helpers.DragTo(fromPortUI.GetGlobalCenter(), toPortUI.GetGlobalCenter());
            yield return null;

            graphView.RebuildUI(GraphModel, Store);
            yield return null;
        }

        protected BasicPlacematModel CreatePlacemat(Rect posAndDim, string title = "", int zOrder = 0)
        {
            return GraphModel.CreatePlacemat(title, posAndDim, zOrder);
        }

        protected BasicStickyNoteModel CreateSticky(string title = "", string contents = "", Rect stickyRect = default)
        {
            return GraphModel.CreateStickyNodeGTF(title, contents, stickyRect);
        }
    }
}
