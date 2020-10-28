using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.Searcher;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    [Serializable]
    class Type0FakeNodeModel : NodeModel, IFakeNode
    {
        public static void AddToSearcherDatabase(GraphElementSearcherDatabase db)
        {
            SearcherItem parent = SearcherItemUtility.GetItemFromPath(db.Items, "Fake");

            parent.AddChild(new GraphNodeModelSearcherItem(
                new NodeSearcherItemData(typeof(Type0FakeNodeModel)),
                data => data.CreateNode<Type0FakeNodeModel>(),
                nameof(Type0FakeNodeModel)
            ));
        }

        public PortModel ExeInput0 { get; private set; }
        public PortModel ExeOutput0 { get; private set; }
        public PortModel Input0 { get; private set; }
        public PortModel Input1 { get; private set; }
        public PortModel Input2 { get; private set; }
        public PortModel Output0 { get; private set; }
        public PortModel Output1 { get; private set; }
        public PortModel Output2 { get; private set; }

        protected override void OnDefineNode()
        {
            ExeInput0 = AddExecutionInputPort("exe0");
            ExeOutput0 = AddExecutionOutputPort("exe0");
            Input0 = AddDataInputPort<int>("input0");
            Input1 = AddDataInputPort<int>("input1");
            Input2 = AddDataInputPort<int>("input2");
            Output0 = AddDataOutputPort<int>("output0");
            Output1 = AddDataOutputPort<int>("output1");
            Output2 = AddDataOutputPort<int>("output2");
        }
    }

    interface IFakeNode : INodeModel {}
}
