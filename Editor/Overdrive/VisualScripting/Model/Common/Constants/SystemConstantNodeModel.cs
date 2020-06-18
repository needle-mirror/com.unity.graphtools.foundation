using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.Model", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class SystemConstantNodeModel : NodeModel, ISystemConstantNodeModel
    {
        [SerializeField]
        TypeHandle m_ReturnType;

        [SerializeField]
        TypeHandle m_DeclaringType;

        [SerializeField]
        string m_Identifier;

        public TypeHandle ReturnType
        {
            get => m_ReturnType;
            set => m_ReturnType = value;
        }

        public TypeHandle DeclaringType
        {
            get => m_DeclaringType;
            set => m_DeclaringType = value;
        }

        public string Identifier
        {
            get => m_Identifier;
            set => m_Identifier = value;
        }

        public override string VariableString => "System constant";
        public override string DataTypeString => DeclaringType.Resolve(Stencil).FriendlyName();

        public IPortModel OutputPort { get; private set; }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            OutputPort = AddDataOutputPort(null, m_ReturnType);
        }

        public IGTFPortModel GTFOutputPort => OutputPort as IGTFPortModel;
    }
}
