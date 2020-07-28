using UnityEditor.GraphToolsFoundation.Overdrive.Model;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public interface IGraphElement
    {
        IGTFGraphElementModel Model { get; }
        Store Store { get; }
        GraphView GraphView { get; }

        void Setup(IGTFGraphElementModel model, Store store, GraphView graphView);
        void BuildUI();
        void UpdateFromModel();
        void SetupBuildAndUpdate(IGTFGraphElementModel model, Store store, GraphView graphView);
    }
}
