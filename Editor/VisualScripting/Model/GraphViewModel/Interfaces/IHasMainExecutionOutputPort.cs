namespace UnityEditor.VisualScripting.GraphViewModel
{
    public interface IHasMainExecutionOutputPort : INodeModel
    {
        IPortModel ExecutionOutputPort { get; }
    }
}
