using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Flags]
    public enum PortModelOptions
    {
        None = 0,
        NoEmbeddedConstant = 1,
        Hidden = 2,
        Default = None,
    }

    [Serializable]
    //[MovedFrom(false, "UnityEditor.VisualScripting.GraphViewModel", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    [MovedFrom("UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel")]
    public class PortModel : IReorderableEdgesPort, IHasTitle
    {
        [SerializeField]
        SerializableGUID m_Guid;

        string m_UniqueId;

        public IGTFGraphAssetModel AssetModel
        {
            get => GraphModel?.AssetModel;
            set => throw new NotImplementedException();
        }

        public IGTFGraphModel GraphModel => NodeModel?.GraphModel;

        public IPortNode NodeModel { get; set; }

        public string Title { get; set; }

        public string DisplayTitle => Title.Nicify();

        public string UniqueName => m_UniqueId ?? Title ?? m_Guid.ToString();

        public PortModelOptions Options { get; private set; }

        public PortType PortType { get; set; }

        public Direction Direction { get; set; }

        public Orientation Orientation { get; set; }

        // Give node model priority over self
        public PortCapacity Capacity => NodeModel?.GetPortCapacity(this) ?? GetDefaultCapacity();

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

        public virtual IEnumerable<IGTFPortModel> GetConnectedPorts()
        {
            return PortModelDefaultImplementations.GetConnectedPorts(this);
        }

        public virtual IEnumerable<IGTFEdgeModel> GetConnectedEdges()
        {
            return PortModelDefaultImplementations.GetConnectedEdges(this);
        }

        public virtual bool IsConnectedTo(IGTFPortModel toPort)
        {
            return PortModelDefaultImplementations.IsConnectedTo(this, toPort);
        }

        public bool HasReorderableEdges => PortType == PortType.Execution && Direction == Direction.Output && this.IsConnected();

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
        }

        public virtual bool CreateEmbeddedValueIfNeeded => PortType == PortType.Data;

        public GUID Guid
        {
            get
            {
                if (m_Guid.GUID.Empty())
                    AssignNewGuid();
                return m_Guid;
            }
            set => m_Guid = value;
        }

        public PortModel(string name = null, string uniqueId = null, PortModelOptions options = PortModelOptions.Default)
        {
            Title = name;
            m_UniqueId = uniqueId;
            Options = options;
        }

        public void AssignNewGuid()
        {
            m_Guid = GUID.Generate();
        }

        public override string ToString()
        {
            return $"Port {NodeModel}: {PortType} {Title}(id: {UniqueName ?? "\"\""})";
        }

        public PortCapacity GetDefaultCapacity()
        {
            return PortType == PortType.Data ? Direction == Direction.Input ? PortCapacity.Single :
                PortCapacity.Multi :
                (PortType == PortType.Execution) ? Direction == Direction.Output ? PortCapacity.Single :
                PortCapacity.Multi :
                PortCapacity.Multi;
        }

        public static bool Equivalent(IGTFPortModel a, IGTFPortModel b)
        {
            if (a == null || b == null)
                return a == b;

            return a.Direction == b.Direction && a.NodeModel.Guid == b.NodeModel.Guid && a.UniqueName == b.UniqueName;
        }

        public virtual void MoveEdgeFirst(IGTFEdgeModel edge)
        {
            ReorderableEdgesPortDefaultImplementations.MoveEdgeFirst(this, edge);
        }

        public virtual void MoveEdgeUp(IGTFEdgeModel edge)
        {
            ReorderableEdgesPortDefaultImplementations.MoveEdgeUp(this, edge);
        }

        public virtual void MoveEdgeDown(IGTFEdgeModel edge)
        {
            ReorderableEdgesPortDefaultImplementations.MoveEdgeDown(this, edge);
        }

        public virtual void MoveEdgeLast(IGTFEdgeModel edge)
        {
            ReorderableEdgesPortDefaultImplementations.MoveEdgeLast(this, edge);
        }
    }
}
