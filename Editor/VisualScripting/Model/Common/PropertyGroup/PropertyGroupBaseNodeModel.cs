using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.EditorCommon.Redux;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.VisualScripting.Model
{
    [Serializable]
    public abstract class PropertyGroupBaseNodeModel : NodeModel, IHasInstancePort
    {
        [SerializeField]
        List<TypeMember> m_Members;

        public IEnumerable<TypeMember> Members => m_Members ?? (m_Members = new List<TypeMember>());

        protected abstract Direction MemberPortDirection { get; }

        public IPortModel InstancePort { get; private set; }

        public IEnumerable<IPortModel> GetPortsForMembers()
        {
            var portsById = MemberPortDirection == Direction.Input ? InputsById : OutputsById;
            return Members.Select(m => portsById[m.GetId()]);
        }

        protected override void OnDefineNode()
        {
            InstancePort = AddInstanceInput(TypeHandle.Unknown);

            foreach (var member in Members)
            {
                if (MemberPortDirection == Direction.Input)
                    AddDataInputPort(member.GetId(), member.Type);
                else
                    AddDataOutputPort(member.GetId(), member.Type);
            }
        }

        public TypeHandle GetConnectedInstanceType()
        {
            if (InstancePort == null || !InstancePort.Connected)
                return TypeHandle.Unknown;

            return InstancePort.DataType;
        }

        public void AddMember(Type type, string memberName)
        {
            Assert.IsNotNull(type);
            Assert.IsNotNull(memberName);
            var member = new TypeMember(type.GenerateTypeHandle(Stencil), new List<string> { memberName });

            m_Members.Add(member);
            DefineNode();
        }

        public override void OnConnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel)
        {
            if (selfConnectedPortModel.Direction != Direction.Input || otherConnectedPortModel == null)
                return;

            if (((PortModel)selfConnectedPortModel).DataType != otherConnectedPortModel.DataType)
            {
                ((PortModel)selfConnectedPortModel).DataType = otherConnectedPortModel.DataType;
                // TODO member types might have changed (ie. new instance type has a member with the same name
                // as the previous one, but a different type: struct A { int x; } / struct B { float x; }
            }
        }

        public override void OnDisconnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel)
        {
            if (selfConnectedPortModel.Direction != Direction.Input || selfConnectedPortModel.PortType != PortType.Instance)
                return;

            ((PortModel)selfConnectedPortModel).DataType = TypeHandle.Unknown;
        }

        public void AddMember(TypeMember member)
        {
            if (m_Members.Contains(member))
                return;

            m_Members.Add(member);
            ((VSGraphModel)GraphModel).LastChanges.ChangedElements.Add(this);
            DefineNode();
        }

        public void RemoveMember(TypeMember member)
        {
            string memberId = member.GetId();
            m_Members.RemoveAll(m => m.GetId() == memberId);

            // disconnect edges
            var portsById = MemberPortDirection == Direction.Input ? InputsById : OutputsById;

            var portModel = portsById[memberId];
            var oppositePortModel = portModel.ConnectionPortModels.FirstOrDefault();

            if (portModel.Direction == Direction.Input)
                ((VSGraphModel)GraphModel).DeleteEdge(portModel, oppositePortModel);
            else
                ((VSGraphModel)GraphModel).DeleteEdge(oppositePortModel, portModel);

            ((VSGraphModel)GraphModel).LastChanges.ChangedElements.Add(this);


            DefineNode();
        }
    }

    class EditPropertyGroupNodeAction : IAction
    {
        public enum EditType
        {
            Add,
            Remove
        }

        public readonly EditType editType;
        public readonly INodeModel nodeModel;
        public readonly TypeMember member;

        public EditPropertyGroupNodeAction(EditType editType, INodeModel nodeModel, TypeMember member)
        {
            this.editType = editType;
            this.nodeModel = nodeModel;
            this.member = member;
        }
    }
}
