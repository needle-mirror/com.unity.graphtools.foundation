namespace UnityEditor.GraphToolsFoundation.Overdrive
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
