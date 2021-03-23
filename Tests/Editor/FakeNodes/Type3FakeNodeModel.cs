using System;
using UnityEditor.VisualScripting.GraphViewModel;

namespace UnityEditor.VisualScriptingTests
{
    [Serializable]
    class Type3FakeNodeModel : NodeModel
    {
        public IPortModel Input { get; private set; }
        public IPortModel Output { get; private set; }

        protected override void OnDefineNode()
        {
            Input = AddDataInputPort<float>("input0");
            Output = AddDataOutputPort<float>("output0");
        }
    }
}
