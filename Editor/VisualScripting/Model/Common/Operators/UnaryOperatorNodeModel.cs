using System;
using System.Linq;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;

namespace UnityEditor.VisualScripting.Model
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    [Serializable]
    public class UnaryOperatorNodeModel : NodeModel, IOperationValidator
    {
        public UnaryOperatorKind Kind;

        public override string Title => Kind.ToString();
        public IPortModel InputPort { get; private set; }
        public IPortModel OutputPort { get; private set; }

        protected override void OnDefineNode()
        {
            var portType = Kind == UnaryOperatorKind.LogicalNot ? TypeHandle.Bool : TypeHandle.Float;
            InputPort = AddDataInputPort("A", portType);

            if (Kind == UnaryOperatorKind.LogicalNot || Kind == UnaryOperatorKind.Minus)
                OutputPort = AddDataOutputPort("Out", portType);
        }

        public virtual bool HasValidOperationForInput(IPortModel _, TypeHandle typeHandle)
        {
            var type = typeHandle.Resolve(Stencil);
            return TypeSystem.GetOverloadedUnaryOperators(type).Contains(Kind);
        }
    }
}
