using System;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.GraphViewModel;

namespace UnityEditor.VisualScripting.Editor
{
    class IfConditionNode : Node
    {
        public IfConditionNode(INodeModel model, Store store, GraphView graphView)
            : base(model, store, graphView)
        {
        }

        protected override void UpdateOutputPortModels()
        {
        }
    }
}
