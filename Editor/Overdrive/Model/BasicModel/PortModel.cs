using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// A model that represents a port in a node.
    /// </summary>
    [Serializable]
    //[MovedFrom(false, "UnityEditor.VisualScripting.GraphViewModel", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    [MovedFrom("UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel")]
    public class PortModel : IReorderableEdgesPort, IHasTitle, ISerializationCallbackReceiver
    {
        [SerializeField]
        SerializableGUID m_Guid;

        [SerializeField]
        List<string> m_SerializedCapabilities;

        string m_UniqueId;

        protected List<Capabilities> m_Capabilities;

        public IGraphAssetModel AssetModel
        {
            get => GraphModel?.AssetModel;
            set => throw new NotImplementedException();
        }

        public virtual IGraphModel GraphModel => NodeModel?.GraphModel;

        public IPortNode NodeModel { get; set; }

        public string Title { get; set; }

        public string DisplayTitle => Title.Nicify();

        public string UniqueName
        {
            get => m_UniqueId ?? Title ?? m_Guid.ToString();
            set => m_UniqueId = value;
        }

        public PortModelOptions Options { get; set; }

        public PortType PortType { get; set; }

        public Direction Direction { get; set; }

        public Orientation Orientation { get; set; }

        // Give node model priority over self
        public virtual PortCapacity Capacity => NodeModel?.GetPortCapacity(this) ?? GetDefaultCapacity();

        public TypeHandle DataTypeHandle { get; set; }

        public Type PortDataType
        {
            get
            {
                Type t = DataTypeHandle.Resolve();
                t = t == typeof(void) || t.ContainsGenericParameters ? typeof(Unknown) : t;
                return t;
            }
        }

        public virtual IEnumerable<IPortModel> GetConnectedPorts()
        {
            return PortModelDefaultImplementations.GetConnectedPorts(this);
        }

        public virtual IEnumerable<IEdgeModel> GetConnectedEdges()
        {
            return PortModelDefaultImplementations.GetConnectedEdges(this);
        }

        public virtual bool IsConnectedTo(IPortModel toPort)
        {
            return PortModelDefaultImplementations.IsConnectedTo(this, toPort);
        }

        public virtual bool HasReorderableEdges => PortType == PortType.Execution && Direction == Direction.Output && this.IsConnected();

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

        public bool DisableEmbeddedValueEditor => this.IsConnected() && GetConnectedPorts().Any(p => p.NodeModel.State == ModelState.Enabled);

        public virtual string ToolTip
        {
            get
            {
                string newTooltip = Direction == Direction.Output ? "Output" : "Input";
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
            set {}
        }

        public virtual bool CreateEmbeddedValueIfNeeded => PortType == PortType.Data;

        /// <summary>
        /// The unique identifier of the port.
        /// </summary>
        public SerializableGUID Guid
        {
            get
            {
                if (!m_Guid.Valid)
                    AssignNewGuid();
                return m_Guid;
            }
            set => m_Guid = value;
        }

        public PortModel()
        {
            InternalInitCapabilities();
        }

        /// <summary>
        /// Assign a newly generated GUID to the model.
        /// </summary>
        public void AssignNewGuid()
        {
            m_Guid = SerializableGUID.Generate();
        }

        public virtual IReadOnlyList<Capabilities> Capabilities => m_Capabilities;

        public override string ToString()
        {
            return $"Port {NodeModel}: {PortType} {Title}(id: {UniqueName ?? "\"\""})";
        }

        public PortCapacity GetDefaultCapacity()
        {
            return PortType == PortType.Data ? Direction == Direction.Input ? PortCapacity.Single :
                PortCapacity.Multi :
                PortCapacity.Multi;
        }

        public virtual void MoveEdgeFirst(IEdgeModel edge)
        {
            ReorderableEdgesPortDefaultImplementations.MoveEdgeFirst(this, edge);
        }

        public virtual void MoveEdgeUp(IEdgeModel edge)
        {
            ReorderableEdgesPortDefaultImplementations.MoveEdgeUp(this, edge);
        }

        public virtual void MoveEdgeDown(IEdgeModel edge)
        {
            ReorderableEdgesPortDefaultImplementations.MoveEdgeDown(this, edge);
        }

        public virtual void MoveEdgeLast(IEdgeModel edge)
        {
            ReorderableEdgesPortDefaultImplementations.MoveEdgeLast(this, edge);
        }

        public void OnBeforeSerialize()
        {
            m_SerializedCapabilities = m_Capabilities?.Select(c => c.Name).ToList() ?? new List<string>();
        }

        public void OnAfterDeserialize()
        {
            if (!m_SerializedCapabilities.Any())
                // If we're reloading an older node
                InitCapabilities();
            else
                m_Capabilities = m_SerializedCapabilities.Select(Overdrive.Capabilities.Get).ToList();
        }

        protected virtual void InitCapabilities()
        {
            InternalInitCapabilities();
        }

        void InternalInitCapabilities()
        {
            m_Capabilities = new List<Capabilities>
            {
                Overdrive.Capabilities.NoCapabilities
            };
        }
    }
}
