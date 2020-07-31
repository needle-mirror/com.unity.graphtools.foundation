using System;
using System.Collections.Generic;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Translators;

namespace UnityEditor.VisualScriptingTests
{
    [Serializable]
    class Type0FakeNodeModel : NodeModel, IFakeNode
    {
        public IPortModel Input0 { get; private set; }
        public IPortModel Input1 { get; private set; }
        public IPortModel Input2 { get; private set; }
        public IPortModel Output0 { get; private set; }
        public IPortModel Output1 { get; private set; }
        public IPortModel Output2 { get; private set; }

        protected override void OnDefineNode()
        {
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
