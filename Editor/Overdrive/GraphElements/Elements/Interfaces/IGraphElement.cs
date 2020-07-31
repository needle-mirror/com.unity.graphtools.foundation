using UnityEditor.GraphToolsFoundation.Overdrive.Model;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public interface IGraphElement
    {
        IGTFGraphElementModel Model { get; }
        Overdrive.Store Store { get; }
        GraphView GraphView { get; }

        GraphElementPartList PartList { get; }

        void Setup(IGTFGraphElementModel model, Overdrive.Store store, GraphView graphView);
        void BuildUI();
        void UpdateFromModel();
        void SetupBuildAndUpdate(IGTFGraphElementModel model, Overdrive.Store store, GraphView graphView);
    }
}
