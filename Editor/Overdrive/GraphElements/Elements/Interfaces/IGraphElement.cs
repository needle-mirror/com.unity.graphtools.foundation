using UnityEditor.GraphToolsFoundation.Overdrive.Model;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public interface IGraphElement
    {
        IGTFGraphElementModel Model { get; }
        IStore Store { get; }
        GraphView GraphView { get; }

        GraphElementPartList PartList { get; }

        void Setup(IGTFGraphElementModel model, IStore store, GraphView graphView);
        void BuildUI();
        void UpdateFromModel();
        void SetupBuildAndUpdate(IGTFGraphElementModel model, IStore store, GraphView graphView);
    }
}
