using System;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.VisualScriptingTests
{
    [Serializable]
    class Type1FakeNodeModel : NodeModel
    {
        public IPortModel Input { get; private set; }
        public IPortModel Output { get; private set; }

        protected override void OnDefineNode()
        {
            Input = AddDataInputPort<GameObject>("input0");
            Output = AddDataOutputPort<GameObject>("output0");
        }
    }
}
