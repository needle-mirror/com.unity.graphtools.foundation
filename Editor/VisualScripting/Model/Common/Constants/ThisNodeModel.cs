using System;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model
{
    [Serializable]
    public class ThisNodeModel : NodeModel, IVariableModel, IRenamableModel
    {
        public IPortModel OutputPort { get; private set; }
        public IVariableDeclarationModel DeclarationModel => null;

        const string k_Title = "This";

        public override string Title => k_Title;

        public override string DataTypeString => GraphModel?.FriendlyScriptName ?? string.Empty;
        public override string VariableString => "Variable";

        protected override void OnDefineNode()
        {
            OutputPort = AddDataOutputPort(null, TypeHandle.ThisType);
        }

        public void Rename(string newName)
        {
            throw new NotImplementedException();
        }
    }
}
