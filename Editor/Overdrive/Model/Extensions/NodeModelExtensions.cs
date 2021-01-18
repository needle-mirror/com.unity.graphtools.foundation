using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public static class NodeModelExtensions
    {
        public static IEnumerable<IPortModel> GetInputPorts(this IInOutPortsNode self)
        {
            return self.InputsById.Values;
        }

        public static IEnumerable<IPortModel> GetOutputPorts(this IInOutPortsNode self)
        {
            return self.OutputsById.Values;
        }

        internal static IEnumerable<IPortModel> ConnectedPortsWithReorderableEdges(this IPortNode self)
        {
            return self.Ports?.OfType<IReorderableEdgesPort>().Where(p => p.HasReorderableEdges)
                ?? Enumerable.Empty<IPortModel>();
        }

        public static void RevealReorderableEdgesOrder(this IPortNode self, bool show, IEdgeModel edgeToShow = null)
        {
            var outputPortsWithReorderableEdges = self.ConnectedPortsWithReorderableEdges();
            if (edgeToShow != null)
            {
                outputPortsWithReorderableEdges = outputPortsWithReorderableEdges.Where(p => p == edgeToShow.FromPort);
            }

            foreach (var portModel in outputPortsWithReorderableEdges)
            {
                ShowEdgeIndex(portModel);
            }

            void ShowEdgeIndex(IPortModel portModel)
            {
                var edges = portModel.GetConnectedEdges().ToList();

                for (int i = 0; i < edges.Count; i++)
                {
                    var edge = edges[i];
                    // Only show when we have more than one edge (i.e. when it's meaningful)
                    edge.EdgeLabel = show && edges.Count > 1 ? (i + 1).ToString() : "";
                }
            }
        }

        public static IPortModel AddPlaceHolderPort(this IInOutPortsNode self, Direction direction, string uniqueId)
        {
            if (direction == Direction.Input)
                return self.AddInputPort(uniqueId, PortType.MissingPort, TypeHandle.MissingPort, uniqueId,
                    PortModelOptions.NoEmbeddedConstant);

            return self.AddOutputPort(uniqueId, PortType.MissingPort, TypeHandle.MissingPort, uniqueId,
                PortModelOptions.NoEmbeddedConstant);
        }

        public static IPortModel AddDataInputPort(this IInOutPortsNode self, string portName, TypeHandle typeHandle, string portId = null,
            PortModelOptions options = PortModelOptions.Default, Action<IConstant> preDefine = null)
        {
            return self.AddInputPort(portName, PortType.Data, typeHandle, portId, options, preDefine);
        }

        public static IPortModel AddDataInputPort<TDataType>(this IInOutPortsNode self, string portName, string portId = null,
            PortModelOptions options = PortModelOptions.Default, TDataType defaultValue = default)
        {
            Action<IConstant> preDefine = null;

            if (defaultValue is Enum || !EqualityComparer<TDataType>.Default.Equals(defaultValue, default))
                preDefine = constantModel => constantModel.ObjectValue = defaultValue;

            return self.AddDataInputPort(portName, typeof(TDataType).GenerateTypeHandle(), portId, options, preDefine);
        }

        public static IPortModel AddDataOutputPort(this IInOutPortsNode self, string portName, TypeHandle typeHandle, string portId = null,
            PortModelOptions options = PortModelOptions.Default)
        {
            return self.AddOutputPort(portName, PortType.Data, typeHandle, portId, options);
        }

        public static IPortModel AddDataOutputPort<TDataType>(this IInOutPortsNode self, string portName, string portId = null)
        {
            return self.AddDataOutputPort(portName, typeof(TDataType).GenerateTypeHandle(), portId);
        }

        public static IPortModel AddExecutionInputPort(this IInOutPortsNode self, string portName, string portId = null)
        {
            return self.AddInputPort(portName, PortType.Execution, TypeHandle.ExecutionFlow, portId);
        }

        public static IPortModel AddExecutionOutputPort(this IInOutPortsNode self, string portName, string portId = null)
        {
            return self.AddOutputPort(portName, PortType.Execution, TypeHandle.ExecutionFlow, portId);
        }

        public static IEnumerable<IPortModel> GetPorts(this IPortNode self, Direction direction, PortType portType)
        {
            return self.Ports.Where(p => (p.Direction & direction) == direction && p.PortType == portType);
        }
    }
}
