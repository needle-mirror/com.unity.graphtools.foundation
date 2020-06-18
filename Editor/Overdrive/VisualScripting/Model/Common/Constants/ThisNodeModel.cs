using System;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.Model", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class ThisNodeModel : NodeModel, IVariableModel
    {
        public IPortModel OutputPort { get; private set; }
        public IVariableDeclarationModel DeclarationModel => null;

        const string k_Title = "This";

        public override string Title => k_Title;

        public override string DataTypeString => VSGraphModel?.FriendlyScriptName ?? string.Empty;
        public override string VariableString => "Variable";

        protected override void OnDefineNode()
        {
            OutputPort = AddDataOutputPort(null, TypeHandle.ThisType);
        }

        public IGTFPortModel GTFInputPort => OutputPort.Direction == Direction.Input ? OutputPort as IGTFPortModel : null;
        public IGTFPortModel GTFOutputPort => OutputPort.Direction == Direction.Output ? OutputPort as IGTFPortModel : null;
    }
}
