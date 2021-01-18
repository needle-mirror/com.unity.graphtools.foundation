using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class TestBlankPage : BlankPage
    {
        public TestBlankPage(Store store)
            : base(store) {}
    }

    class TestGraphViewWindow : GtfoWindow
    {
        public IGraphModel GraphModel { get; private set; }

        public TestGraphViewWindow()
        {
            this.SetDisableInputEvents(true);
            WithSidePanel = false;
        }

        protected override bool CanHandleAssetType(GraphAssetModel asset)
        {
            return true;
        }

        protected override Overdrive.State CreateInitialState()
        {
            GraphModel = new GraphModel {StencilType = typeof(TestStencil)};
            return new State(GUID, GraphModel);
        }

        protected override BlankPage CreateBlankPage()
        {
            return new TestBlankPage(Store);
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
            return new TestGraphView(this, Store);
        }

        protected override Dictionary<Event, ShortcutDelegate> GetShortcutDictionary()
        {
            return new Dictionary<Event, ShortcutDelegate>();
        }
    }
}
