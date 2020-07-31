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
    public class ThisNodeModel : NodeModel, IGTFVariableNodeModel, IHasMainOutputPort
    {
        public IGTFPortModel MainOutputPort { get; private set; }
        public IGTFVariableDeclarationModel VariableDeclarationModel => null;

        const string k_Title = "This";

        public override string Title => k_Title;

        public override string DataTypeString => GraphModel?.FriendlyScriptName ?? string.Empty;
        public override string VariableString => "Variable";

        protected override void OnDefineNode()
        {
            MainOutputPort = AddDataOutputPort(null, VSTypeHandle.ThisType);
        }

        public IGTFPortModel InputPort => MainOutputPort?.Direction == Direction.Input ? MainOutputPort : null;
        public IGTFPortModel OutputPort => MainOutputPort?.Direction == Direction.Output ? MainOutputPort : null;
    }
}
