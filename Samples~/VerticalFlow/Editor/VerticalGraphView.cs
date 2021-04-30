namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Vertical
{
    class VerticalGraphView : GraphView
    {
        public VerticalGraphView(GraphViewEditorWindow window, CommandDispatcher commandDispatcher, string graphViewName)
            : base(window, commandDispatcher, graphViewName)
        {
            SetupZoom(0.05f, 5.0f, 5.0f);
        }
    }
}
