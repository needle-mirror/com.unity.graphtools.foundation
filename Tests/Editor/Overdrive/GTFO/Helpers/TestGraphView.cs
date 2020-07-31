using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.Helpers
{
    public class TestGraphView : GraphView
    {
        public TestGraphView(Overdrive.GraphElements.Store store) : base(store)
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            focusable = true;
        }
    }
}
