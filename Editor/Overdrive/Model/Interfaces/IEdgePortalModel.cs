namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IEdgePortalModel : INodeModel, IHasTitle, IHasDeclarationModel
    {
        int EvaluationOrder { get; }
        bool CanCreateOppositePortal();
    }

    public interface IEdgePortalEntryModel : IEdgePortalModel, ISingleInputPortNodeModel
    {
    }

    public interface IEdgePortalExitModel : IEdgePortalModel, ISingleOutputPortNodeModel
    {
    }
}
