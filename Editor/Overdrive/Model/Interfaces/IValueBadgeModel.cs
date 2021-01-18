namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IValueBadgeModel : IBadgeModel
    {
        IPortModel ParentPortModel { get; }
        string DisplayValue { get; }
    }
}
