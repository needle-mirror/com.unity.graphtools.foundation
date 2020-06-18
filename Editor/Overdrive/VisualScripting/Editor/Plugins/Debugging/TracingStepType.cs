namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Plugins
{
    public enum TracingStepType : byte
    {
        None,
        ExecutedNode,
        TriggeredPort,
        WrittenValue,
        ReadValue,
        Error
    }
}
