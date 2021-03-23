using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model
{
    [Serializable]
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    // virtual PortModel getters to allow for Moq
    public class BinaryOperatorNodeModel : NodeModel, IOperationValidator, IHasMainOutputPort
    {
        public BinaryOperatorKind Kind;

        static Type[] s_SortedNumericTypes =
        {
            typeof(byte),
            typeof(sbyte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(float),
            typeof(long),
            typeof(ulong),
            typeof(double),
            typeof(decimal)
        };

        public override string Title => Kind.ToString();

        public enum PortName
        {
            PortA, PortB
        }

        PortModel m_InputAPort;
        PortModel m_InputBPort;
        PortModel m_MainOutputPort;

        // virtual to allow for Moq
        public virtual IPortModel InputPortA => m_InputAPort;
        public virtual IPortModel InputPortB => m_InputBPort;
        public virtual IPortModel OutputPort => m_MainOutputPort;

        public IPortModel GetPort(PortName portName)
        {
            return portName == PortName.PortA ? InputPortA : InputPortB;
        }

        protected override void OnDefineNode()
        {
            var portsType = TypeHandle.Float;
            switch (Kind)
            {
                case BinaryOperatorKind.BitwiseAnd:
                case BinaryOperatorKind.BitwiseOr:
                    portsType = TypeHandle.Int;
                    break;

                case BinaryOperatorKind.LogicalAnd:
                case BinaryOperatorKind.LogicalOr:
                case BinaryOperatorKind.Xor:
                    portsType = TypeHandle.Bool;
                    break;
            }

            if (m_InputAPort == null) // node was never defined
            {
                DefinePorts(portsType, portsType, portsType);
            }
            else // we might have redefined types already
            {
                DefinePorts(m_InputAPort.DataType, m_InputBPort.DataType, m_MainOutputPort.DataType);
            }
        }

        void DefinePorts(TypeHandle aType, TypeHandle bType, TypeHandle outputType)
        {
            m_InputAPort = AddDataInputPort("A", aType, nameof(PortName.PortA));
            m_InputBPort = AddDataInputPort("B", bType, nameof(PortName.PortB));
            m_MainOutputPort = AddDataOutputPort("Out", outputType);
        }

        static bool IsBooleanOperatorKind(BinaryOperatorKind kind)
        {
            switch (kind)
            {
                case BinaryOperatorKind.Equals:
                case BinaryOperatorKind.NotEqual:
                case BinaryOperatorKind.GreaterThan:
                case BinaryOperatorKind.GreaterThanOrEqual:
                case BinaryOperatorKind.LessThan:
                case BinaryOperatorKind.LessThanOrEqual:
                    return true;
            }
            return false;
        }

        static int GetNumericTypePriority(Type type)
        {
            return Array.IndexOf(s_SortedNumericTypes, type);
        }

        static Type GetBiggestNumericType(Type x, Type y)
        {
            return GetNumericTypePriority(x) > GetNumericTypePriority(y) ? x : y;
        }

        public static Type GetOutputTypeFromInputs(BinaryOperatorKind kind, Type x, Type y)
        {
            if (x == typeof(Unknown))
                x = null;
            if (y == typeof(Unknown))
                y = null;
            List<MethodInfo> operators = TypeSystem.GetBinaryOperators(kind, x, y);
            if (IsBooleanOperatorKind(kind))
                return operators.Any() ? operators[0].ReturnType : typeof(bool);

            // TODO handle multiplying numeric types together: float*float=double? etc.
            // An idea was to use Roslyn to generate a lookup table for arithmetic operations
            if (operators.Count >= 1 && operators.All(o => o.ReturnType == operators[0].ReturnType)) // all operators have the same return type
                return operators[0].ReturnType;
            if (x == null && y == null)                // both null
                return typeof(Unknown);
            if (x == null || y == null)                // one is null
                return x ?? y;
            if (x == y)                                // same type
                return x;
            if (x.IsNumeric() && y.IsNumeric())        // both numeric types
                return GetBiggestNumericType(x, y);

            return typeof(Unknown);
        }

        public override void OnConnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel)
        {
            if (selfConnectedPortModel.Direction != Direction.Input)
                return;
            PortModel portToModify = ReferenceEquals(selfConnectedPortModel, InputPortA) ? m_InputAPort : m_InputBPort;
            if (portToModify != null && otherConnectedPortModel != null)
                portToModify.DataType = otherConnectedPortModel.DataType;

            UpdateOutputPortType();
        }

        public override void OnDisconnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel)
        {
            if (selfConnectedPortModel.Direction != Direction.Input)
                return;
            PortModel portToModify = ReferenceEquals(selfConnectedPortModel, InputPortA) ? m_InputAPort : m_InputBPort;
            if (portToModify != null)
                portToModify.DataType = TypeHandle.Float;

            UpdateOutputPortType();
        }

        void UpdateOutputPortType()
        {
            Type portAType = InputPortA?.DataType.Resolve(Stencil);
            Type portBType = InputPortB?.DataType.Resolve(Stencil);

            //TODO A bit ugly of a hack... evaluate a better approach?
            m_MainOutputPort.DataType = GetOutputTypeFromInputs(Kind, portAType, portBType).GenerateTypeHandle(Stencil);
        }

        public bool HasValidOperationForInput(IPortModel port, TypeHandle typeHandle)
        {
            PortName portName = ReferenceEquals(port, InputPortA) ? PortName.PortA : PortName.PortB;
            var otherPort = portName == PortName.PortA ? InputPortB : InputPortA;
            var dataType = typeHandle.Resolve(Stencil);

            if (otherPort.Connected)
            {
                Type otherPortType = otherPort.DataType.Resolve(Stencil);

                return portName == PortName.PortB
                    ? TypeSystem.IsBinaryOperationPossible(otherPortType, dataType, Kind)
                    : TypeSystem.IsBinaryOperationPossible(dataType, otherPortType, Kind);
            }

            return TypeSystem.GetOverloadedBinaryOperators(dataType).Contains(Kind);
        }
    }
}
