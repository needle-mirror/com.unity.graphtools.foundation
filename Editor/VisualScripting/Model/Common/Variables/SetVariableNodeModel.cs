using System;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;

namespace UnityEditor.VisualScripting.Model
{
    [SearcherItem(typeof(ClassStencil), SearcherContext.Stack, "Variable/Set Variable")]
    [Serializable]
    public class SetVariableNodeModel : NodeModel
    {
        const string k_Title = "Set Variable";

        public override string Title => k_Title;

        public PortModel InstancePort { get; private set; }
        public PortModel ValuePort { get; private set; }

        protected override void OnDefineNode()
        {
            InstancePort = AddInstanceInput<Unknown>(null, "Instance");
            ValuePort = AddDataInputPort<Unknown>("Value");
        }

        public override void OnConnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel)
        {
            if (ReferenceEquals(InstancePort, selfConnectedPortModel))
            {
                TypeHandle t = otherConnectedPortModel?.DataType ?? TypeHandle.Unknown;
                InstancePort.DataType = t;
                ValuePort.DataType = t;
            }
        }

        public override void OnDisconnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel)
        {
            OnConnection(selfConnectedPortModel, null);
        }
    }
}
