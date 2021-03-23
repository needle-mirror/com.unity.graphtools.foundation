using System;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model
{
    [Serializable]
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
    }
}
