namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IEdgePortalModel : INodeModel, IHasTitle, IHasDeclarationModel
    {
        int EvaluationOrder { get; }
        bool CanCreateOppositePortal();
    }

    public interface IEdgePortalEntryModel : IEdgePortalModel, ISingleInputPortNode
    {
    }

    public interface IEdgePortalExitModel : IEdgePortalModel, ISingleOutputPortNode
    {
    }
}
