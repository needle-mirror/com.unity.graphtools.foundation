namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// An interface for the UI that represents a <see cref="IGraphElementModel"/> in a <see cref="GraphView"/>.
    /// </summary>
    public interface IGraphElement : IModelUI
    {
        GraphView GraphView { get; }
    }
}
