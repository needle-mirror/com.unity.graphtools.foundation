namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Any UI based on a model, i.e. graph elements but also ports and blackboard elements
    /// </summary>
    public interface IModelUI
    {
        IGraphElementModel Model { get; }
        CommandDispatcher CommandDispatcher { get; }
        // Vlad: TODO move Graphview and related members to IGraphElementModel
        GraphView GraphView { get; }
        string Context { get; }

        void AddToGraphView(GraphView graphView);
        void RemoveFromGraphView();

        void Setup(IGraphElementModel model, CommandDispatcher commandDispatcher, GraphView graphView, string context);
        void BuildUI();
        void UpdateFromModel();
        void SetupBuildAndUpdate(IGraphElementModel model, CommandDispatcher commandDispatcher, GraphView graphView, string context);

        void AddBackwardDependencies();
        void AddForwardDependencies();
        void AddModelDependencies();
    }
}
