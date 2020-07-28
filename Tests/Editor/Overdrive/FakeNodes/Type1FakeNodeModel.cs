using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    [Serializable]
    class Type1FakeNodeModel : NodeModel
    {
        public IGTFPortModel Input { get; private set; }
        public IGTFPortModel Output { get; private set; }

        protected override void OnDefineNode()
        {
            Input = AddDataInputPort<GameObject>("input0");
            Output = AddDataOutputPort<GameObject>("output0");
        }
    }
}
