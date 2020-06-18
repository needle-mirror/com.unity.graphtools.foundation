using System;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch;
using UnityEditor.Searcher;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    [Serializable]
    class Type3FakeNodeModel : NodeModel
    {
        public static void AddToSearcherDatabase(GraphElementSearcherDatabase db)
        {
            SearcherItem parent = SearcherItemUtility.GetItemFromPath(db.Items, "Fake");

            parent.AddChild(new GraphNodeModelSearcherItem(
                new NodeSearcherItemData(typeof(Type3FakeNodeModel)),
                data => data.CreateNode<Type3FakeNodeModel>(),
                nameof(Type3FakeNodeModel)
            ));
        }

        public IPortModel Input { get; private set; }
        public IPortModel Output { get; private set; }

        protected override void OnDefineNode()
        {
            Input = AddDataInputPort<float>("input0");
            Output = AddDataOutputPort<float>("output0");
        }
    }
}
