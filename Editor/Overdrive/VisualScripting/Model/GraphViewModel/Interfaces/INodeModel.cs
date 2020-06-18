using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel
{
    public interface IPropertyVisitorNodeTarget
    {
        object Target { get; set; }
        bool IsExcluded(object value);
    }

    public interface IMigratePorts : INodeModel
    {
        bool MigratePort(ref string portReferenceUniqueId, Direction direction);
    }

    public interface INodeModel : IGraphElementModelWithGuid, IUndoRedoAware
    {
        Vector2 Position { get; set; }
        ModelState State { get; }
        string Title { get; }
        GUID Guid { get; }
        IReadOnlyDictionary<string, IPortModel> InputsById { get; }
        IReadOnlyDictionary<string, IPortModel> OutputsById { get; }
        IReadOnlyList<IPortModel> InputsByDisplayOrder { get; }
        IReadOnlyList<IPortModel> OutputsByDisplayOrder { get; }
        bool IsCondition { get; }
        Color Color { get; set; }
        bool HasUserColor { get; set; }
        bool Destroyed { get; }
        string ToolTip { get; }
        bool HasProgress { get; }

        void OnConnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel);
        void OnDisconnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel);

        void PostGraphLoad();

        PortCapacity GetPortCapacity(PortModel portModel);
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class INodeModelExtensions
    {
        public static IEnumerable<IPortModel> GetPortModels(this INodeModel node)
        {
            return node.InputsByDisplayOrder.Concat(node.OutputsByDisplayOrder);
        }

        public static IEnumerable<IEdgeModel> GetConnectedEdges(this INodeModel nodeModel)
        {
            var graphModel = nodeModel.VSGraphModel;
            return nodeModel.GetPortModels().SelectMany(p => graphModel.GetEdgesConnections(p));
        }

        public static IEnumerable<INodeModel> GetConnectedNodes(this INodeModel nodeModel)
        {
            foreach (IPortModel portModel in nodeModel.GetPortModels())
            {
                foreach (IPortModel connectionPortModel in portModel.ConnectionPortModels)
                {
                    yield return connectionPortModel.NodeModel;
                }
            }
        }

        public static IPortModel GetPortFitToConnectTo(this INodeModel nodeModel, IPortModel portModel)
        {
            // PF: FIXME: This should be the same as GraphView.GetCompatiblePorts (which will move to GraphModel soon).
            // It should also be coherent with the nodes presented in the searcher.

            var portsToChooseFrom = portModel.Direction == Direction.Input ? nodeModel.OutputsByDisplayOrder : nodeModel.InputsByDisplayOrder;
            return GetFirstPortModelOfType(portModel.PortType, portModel.DataTypeHandle, portsToChooseFrom);
        }

        static IPortModel GetFirstPortModelOfType(PortType portType, TypeHandle typeHandle, IReadOnlyList<IPortModel> portModels)
        {
            if (typeHandle != TypeHandle.Unknown && portModels.Any())
            {
                Stencil stencil = portModels.First().VSGraphModel.Stencil;
                IPortModel unknownPortModel = null;

                // Return the first matching Input portModel
                // If no match was found, return the first Unknown typed portModel
                // Else return null.
                foreach (IPortModel portModel in portModels.Where(p => p.PortType == portType))
                {
                    if (portModel.DataTypeHandle == TypeHandle.Unknown && unknownPortModel == null)
                    {
                        unknownPortModel = portModel;
                    }

                    if (typeHandle.IsAssignableFrom(portModel.DataTypeHandle, stencil))
                    {
                        return portModel;
                    }
                }

                if (unknownPortModel != null)
                    return unknownPortModel;
            }

            return null;
        }
    }
}
