namespace UnityEditor.VisualScripting.GraphViewModel
{
    public interface IEdgePortalModel : INodeModel
    {
        int PortalID { get; }
        int EvaluationOrder { get; }
    }

    public interface IEdgePortalEntryModel : IEdgePortalModel
    {
        IPortModel InputPort { get; }
    }

    public interface IEdgePortalExitModel : IEdgePortalModel
    {
        IPortModel OutputPort { get; }
    }
}
