namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IGraphElement
    {
        IGraphElementModel Model { get; }
        Store Store { get; }
        GraphView GraphView { get; }

        void Setup(IGraphElementModel model, Store store, GraphView graphView);
        void BuildUI();
        void UpdateFromModel();
        void SetupBuildAndUpdate(IGraphElementModel model, Store store, GraphView graphView);
    }
}
