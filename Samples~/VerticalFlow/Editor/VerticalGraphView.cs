namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Vertical
{
    class VerticalGraphView : GraphView
    {
        public override bool SupportsWindowedBlackboard => false;

        public VerticalGraphView(GraphViewEditorWindow window, CommandDispatcher commandDispatcher)
            : base(window, commandDispatcher)
        {
            SetupZoom(0.05f, 5.0f, 5.0f);
            name = "Vertical Graph View";
        }
    }
}
