namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IGraphElement
    {
        IGraphElementModel Model { get; }
        Store Store { get; }
        GraphView GraphView { get; }
        string Context { get; }

        void AddToGraphView(GraphView graphView);
        void RemoveFromGraphView();

        void Setup(IGraphElementModel model, Store store, GraphView graphView, string context);
        void BuildUI();
        void UpdateFromModel();
        void SetupBuildAndUpdate(IGraphElementModel model, Store store, GraphView graphView, string context);

        void AddBackwardDependencies();
        void AddForwardDependencies();
        void AddModelDependencies();
    }
}
