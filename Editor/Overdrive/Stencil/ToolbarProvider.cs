namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class ToolbarProvider : IToolbarProvider
    {
        public bool ShowButton(string buttonName)
        {
            return buttonName != MainToolbar.EnableTracingButton && buttonName != MainToolbar.BuildAllButton;
        }
    }
}
