using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// A model that represents a port in a node.
    /// </summary>
    [Serializable]
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class PortModel : GraphElementModel, IReorderableEdgesPortModel, IHasTitle
    {
        string m_UniqueId;

        /// <inheritdoc />
        public IPortNodeModel NodeModel { get; set; }

        /// <inheritdoc />
        public string Title { get; set; }

        /// <inheritdoc />
        public virtual string DisplayTitle => Title.Nicify();

        /// <inheritdoc />
        public string UniqueName
        {
            get => m_UniqueId ?? Title ?? Guid.ToString();
            set => m_UniqueId = value;
        }

        /// <inheritdoc />
        public PortModelOptions Options { get; set; }

        /// <inheritdoc />
        public PortType PortType { get; set; }

        /// <inheritdoc />
        public PortDirection Direction { get; set; }

        /// <inheritdoc />
        public PortOrientation Orientation { get; set; }

        /// <inheritdoc />
        public virtual PortCapacity Capacity => NodeModel?.GetPortCapacity(this) ?? GetDefaultCapacity();

        /// <inheritdoc />
        public TypeHandle DataTypeHandle { get; set; }

        /// <inheritdoc />
        public Type PortDataType
        {
            get
            {
                Type t = DataTypeHandle.Resolve();
                t = t == typeof(void) || t.ContainsGenericParameters ? typeof(Unknown) : t;
                return t;
            }
        }

        /// <inheritdoc />
        public virtual IEnumerable<IPortModel> GetConnectedPorts()
        {
            return PortModelDefaultImplementations.GetConnectedPorts(this);
        }

        /// <inheritdoc />
        public virtual IEnumerable<IEdgeModel> GetConnectedEdges()
        {
            return PortModelDefaultImplementations.GetConnectedEdges(this);
        }

        /// <inheritdoc />
        public virtual bool IsConnectedTo(IPortModel toPort)
        {
            return PortModelDefaultImplementations.IsConnectedTo(this, toPort);
        }

        /// <inheritdoc />
        public virtual bool HasReorderableEdges => PortType == PortType.Execution && Direction == PortDirection.Output && this.IsConnected();

        /// <inheritdoc />
        public IConstant EmbeddedValue
        {
            get
            {
                if (NodeModel is NodeModel node && node.InputConstantsById.TryGetValue(UniqueName, out var inputModel))
                {
                    return inputModel;
                }

                return null;
            }
        }

        /// <inheritdoc />
        public bool DisableEmbeddedValueEditor => this.IsConnected() && GetConnectedPorts().Any(p => p.NodeModel.State == ModelState.Enabled);

        /// <inheritdoc />
        public virtual string ToolTip
        {
            get
            {
                string newTooltip = Direction == PortDirection.Output ? "Output" : "Input";
                if (PortType == PortType.Execution)
                {
                    newTooltip += " execution flow";
                }
                else if (PortType == PortType.Data)
                {
                    var stencil = GraphModel.Stencil;
                    newTooltip += $" of type {DataTypeHandle.GetMetadata(stencil).FriendlyName}";
                }

                return newTooltip;
            }

            // We don't support setting the tooltip for base port models.
            set { }
        }

        /// <inheritdoc />
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            if (AssetModel == null && NodeModel != null)
                AssetModel = NodeModel.AssetModel;
        }

        /// <inheritdoc />
        public virtual bool CreateEmbeddedValueIfNeeded => PortType == PortType.Data;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Port {NodeModel}: {PortType} {Title}(id: {UniqueName ?? "\"\""})";
        }

        /// <inheritdoc />
        public PortCapacity GetDefaultCapacity()
        {
            return PortType == PortType.Data ? Direction == PortDirection.Input ? PortCapacity.Single :
                PortCapacity.Multi :
                PortCapacity.Multi;
        }

        /// <inheritdoc />
        public virtual void MoveEdgeFirst(IEdgeModel edge)
        {
            ReorderableEdgesPortDefaultImplementations.MoveEdgeFirst(this, edge);
        }

        /// <inheritdoc />
        public virtual void MoveEdgeUp(IEdgeModel edge)
        {
            ReorderableEdgesPortDefaultImplementations.MoveEdgeUp(this, edge);
        }

        /// <inheritdoc />
        public virtual void MoveEdgeDown(IEdgeModel edge)
        {
            ReorderableEdgesPortDefaultImplementations.MoveEdgeDown(this, edge);
        }

        /// <inheritdoc />
        public virtual void MoveEdgeLast(IEdgeModel edge)
        {
            ReorderableEdgesPortDefaultImplementations.MoveEdgeLast(this, edge);
        }

        /// <inheritdoc />
        public int GetEdgeOrder(IEdgeModel edge)
        {
            return ReorderableEdgesPortDefaultImplementations.GetEdgeOrder(this, edge);
        }
    }
}
