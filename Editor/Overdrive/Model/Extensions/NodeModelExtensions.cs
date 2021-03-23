using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Extension methods for different types of node model interfaces.
    /// </summary>
    public static class NodeModelExtensions
    {
        public static IEnumerable<IPortModel> GetInputPorts(this IInputOutputPortsNodeModel self)
        {
            return self.InputsById.Values;
        }

        public static IEnumerable<IPortModel> GetOutputPorts(this IInputOutputPortsNodeModel self)
        {
            return self.OutputsById.Values;
        }

        /// <summary>
        /// Adds a new placeholder port on a node.
        /// </summary>
        /// <param name="self">The node to add the new port on.</param>
        /// <param name="direction">The direction of the port the create.</param>
        /// <param name="uniqueId">The ID of the port to create.</param>
        /// <param name="orientation">The orientation of the port to create.</param>
        /// <returns>The newly created placeholder port.</returns>
        public static IPortModel AddPlaceHolderPort(this IInputOutputPortsNodeModel self, Direction direction, string uniqueId,
            Orientation orientation = Orientation.Horizontal)
        {
            if (direction == Direction.Input)
                return self.AddInputPort(uniqueId, PortType.MissingPort, TypeHandle.MissingPort, uniqueId, orientation,
                    PortModelOptions.NoEmbeddedConstant);

            return self.AddOutputPort(uniqueId, PortType.MissingPort, TypeHandle.MissingPort, uniqueId, orientation,
                PortModelOptions.NoEmbeddedConstant);
        }

        /// <summary>
        /// Adds a new data input port on a node.
        /// </summary>
        /// <param name="self">The node to add the new port on.</param>
        /// <param name="portName">The name of port to create.</param>
        /// <param name="dataType">The type of data the port to create handles.</param>
        /// <param name="portId">The ID of the port to create.</param>
        /// <param name="orientation">The orientation of the port to create.</param>
        /// <param name="options">The options for the port to create.</param>
        /// <param name="initializationCallback">An initialization method for the associated constant (if one is needed
        /// for the port) to be called right after the port is created.</param>
        /// <returns>The newly created data input port.</returns>
        public static IPortModel AddDataInputPort(this IInputOutputPortsNodeModel self, string portName, TypeHandle dataType,
            string portId = null, Orientation orientation = Orientation.Horizontal,
            PortModelOptions options = PortModelOptions.Default, Action<IConstant> initializationCallback = null)
        {
            return self.AddInputPort(portName, PortType.Data, dataType, portId, orientation, options, initializationCallback);
        }

        /// <summary>
        /// Adds a new data input port on a node.
        /// </summary>
        /// <param name="self">The node to add the new port on.</param>
        /// <param name="portName">The name of port to create.</param>
        /// <param name="portId">The ID of the port to create.</param>
        /// <param name="orientation">The orientation of the port to create.</param>
        /// <param name="options">The options for the port to create.</param>
        /// <param name="defaultValue">The default value to assign to the constant associated to the port.</param>
        /// <typeparam name="TDataType">The type of data the port to create handles.</typeparam>
        /// <returns>The newly created data input port.</returns>
        public static IPortModel AddDataInputPort<TDataType>(this IInputOutputPortsNodeModel self, string portName,
            string portId = null, Orientation orientation = Orientation.Horizontal,
            PortModelOptions options = PortModelOptions.Default, TDataType defaultValue = default)
        {
            Action<IConstant> initializationCallback = null;

            if (defaultValue is Enum || !EqualityComparer<TDataType>.Default.Equals(defaultValue, default))
                initializationCallback = constantModel => constantModel.ObjectValue = defaultValue;

            return self.AddDataInputPort(portName, typeof(TDataType).GenerateTypeHandle(), portId, orientation, options, initializationCallback);
        }

        /// <summary>
        /// Adds a new data output port on a node.
        /// </summary>
        /// <param name="self">The node to add the new port on.</param>
        /// <param name="portName">The name of port to create.</param>
        /// <param name="dataType">The type of data the port to create handles.</param>
        /// <param name="portId">The ID of the port to create.</param>
        /// <param name="orientation">The orientation of the port to create.</param>
        /// <param name="options">The options for the port to create.</param>
        /// <returns>The newly created data output port.</returns>
        public static IPortModel AddDataOutputPort(this IInputOutputPortsNodeModel self, string portName, TypeHandle dataType,
            string portId = null, Orientation orientation = Orientation.Horizontal,
            PortModelOptions options = PortModelOptions.Default)
        {
            return self.AddOutputPort(portName, PortType.Data, dataType, portId, orientation, options);
        }

        /// <summary>
        /// Adds a new data output port on a node.
        /// </summary>
        /// <param name="self">The node to add the new port on.</param>
        /// <param name="portName">The name of port to create.</param>
        /// <param name="portId">The ID of the port to create.</param>
        /// <param name="orientation">The orientation of the port to create.</param>
        /// <typeparam name="TDataType">The type of data the port to create handles.</typeparam>
        /// <returns>The newly created data output port.</returns>
        public static IPortModel AddDataOutputPort<TDataType>(this IInputOutputPortsNodeModel self, string portName,
            string portId = null, Orientation orientation = Orientation.Horizontal)
        {
            return self.AddDataOutputPort(portName, typeof(TDataType).GenerateTypeHandle(), portId, orientation);
        }

        /// <summary>
        /// Adds a new execution input port on a node.
        /// </summary>
        /// <param name="self">The node to add the new port on.</param>
        /// <param name="portName">The name of port to create.</param>
        /// <param name="portId">The ID of the port to create.</param>
        /// <param name="orientation">The orientation of the port to create.</param>
        /// <returns>The newly created execution input port.</returns>
        public static IPortModel AddExecutionInputPort(this IInputOutputPortsNodeModel self, string portName,
            string portId = null, Orientation orientation = Orientation.Horizontal)
        {
            return self.AddInputPort(portName, PortType.Execution, TypeHandle.ExecutionFlow, portId, orientation);
        }

        /// <summary>
        /// Adds a new execution output port on a node.
        /// </summary>
        /// <param name="self">The node to add the new port on.</param>
        /// <param name="portName">The name of port to create.</param>
        /// <param name="portId">The ID of the port to create.</param>
        /// <param name="orientation">The orientation of the port to create.</param>
        /// <returns>The newly created execution output port.</returns>
        public static IPortModel AddExecutionOutputPort(this IInputOutputPortsNodeModel self, string portName,
            string portId = null, Orientation orientation = Orientation.Horizontal)
        {
            return self.AddOutputPort(portName, PortType.Execution, TypeHandle.ExecutionFlow, portId, orientation);
        }

        public static IEnumerable<IPortModel> GetPorts(this IPortNodeModel self, Direction direction, PortType portType)
        {
            return self.Ports.Where(p => (p.Direction & direction) == direction && p.PortType == portType);
        }
    }
}
