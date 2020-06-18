using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Interfaces;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class ToolbarProvider : IToolbarProvider
    {
        public bool ShowButton(string buttonName)
        {
            return true;
        }
    }
}
