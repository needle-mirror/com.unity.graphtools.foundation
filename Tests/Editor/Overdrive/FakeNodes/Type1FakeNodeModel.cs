using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
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
