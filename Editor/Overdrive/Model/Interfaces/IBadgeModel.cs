namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IBadgeModel : IGraphElementModel
    {
        IGraphElementModel ParentModel { get; }
    }
}
