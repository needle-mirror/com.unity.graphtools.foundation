namespace UnityEditor.VisualScripting.GraphViewModel
{
    public interface IHasMainExecutionInputPort : INodeModel
    {
        IPortModel ExecutionInputPort { get; }
    }
}
