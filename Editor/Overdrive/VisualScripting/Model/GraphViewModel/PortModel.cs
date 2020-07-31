using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel
{
    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.GraphViewModel", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class PortModel : IGTFPortModel, IHasTitle
    {
        [Flags]
        public enum PortModelOptions
        {
            None = 0,
            NoEmbeddedConstant = 1,
            Hidden = 2,
            Default = None,
        }

        public static bool Equivalent(IGTFPortModel a, IGTFPortModel b)
        {
            if (a == null || b == null)
                return a == b;

            return a.Direction == b.Direction && a.NodeModel.Guid == b.NodeModel.Guid && a.UniqueName == b.UniqueName;
        }

        [UsedImplicitly]
        public PortModelOptions Options { get; protected set; }

        TypeHandle m_DataType;

        public PortModel(string name = null, string uniqueId = null, PortModelOptions options = PortModelOptions.Default)
        {
            m_Name = name;
            m_UniqueId = uniqueId;
            Options = options;
        }

        public IGTFGraphAssetModel AssetModel => GraphModel?.AssetModel;
        public IGTFGraphModel GraphModel => NodeModel?.GraphModel;

        string m_Name;

        string m_UniqueId;

        public string UniqueName => m_UniqueId ?? m_Name ?? m_Guid.ToString();

        [SerializeField]
        SerializableGUID m_Guid;

        public GUID Guid
        {
            get
            {
                if (m_Guid.GUID.Empty())
                    AssignNewGuid();
                return m_Guid;
            }
        }

        public void AssignNewGuid()
        {
            m_Guid = GUID.Generate();
        }

        public string Title
        {
            get => m_Name;
            set => m_Name = value;
        }

        public string DisplayTitle => m_Name.Nicify();

        IGTFNodeModel m_NodeModel;

        public IGTFNodeModel NodeModel
        {
            get => m_NodeModel;
            set => m_NodeModel = value;
        }

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

        public bool DisableEmbeddedValueEditor => IsConnected && ConnectionPortModels.Any(p => p.NodeModel.State == ModelState.Enabled);

        public virtual bool CreateEmbeddedValueIfNeeded => PortType == PortType.Data;

        public IEnumerable<IGTFPortModel> ConnectionPortModels
        {
            get
            {
                Assert.IsNotNull(GraphModel, $"portModel {Title} has a null GraphModel reference");
                return GraphModel.GetConnections(this);
            }
        }

        PortType m_PortType;

        public PortType PortType
        {
            get => m_PortType;
            set => m_PortType = value;
        }

        Direction m_Direction;

        public Direction Direction
        {
            get => m_Direction;
            set => m_Direction = value;
        }

        // PF: Is Orientation ever set?
        public Orientation Orientation { get; set; }

        // Give node model priority over self
        public PortCapacity Capacity => NodeModel?.GetPortCapacity(this) ?? GetDefaultCapacity();

        public bool IsConnected => ConnectionPortModels.Any();
        public bool IsConnectedTo(IGTFPortModel port)
        {
            var edgeModels = GraphModel.EdgeModels.Where(e =>
                e.ToPort == this && e.FromPort == port ||
                e.FromPort == this && e.ToPort == port);
            return edgeModels.Any();
        }

        public bool HasReorderableEdges => PortType == PortType.Execution && Direction == Direction.Output && IsConnected;
        public IEnumerable<IGTFEdgeModel> ConnectedEdges => GraphModel.EdgeModels.Where(e => e.ToPort == this || e.FromPort == this);

        public TypeHandle DataTypeHandle
        {
            get => m_DataType;
            set => m_DataType = value;
        }

        public Type PortDataType
        {
            get
            {
                Type t = DataTypeHandle.Resolve();
                t = t == typeof(void) || t.ContainsGenericParameters ? typeof(Unknown) : t;
                return t;
            }
        }

        public override string ToString()
        {
            return $"Port {NodeModel}: {PortType} {Title}(id: {UniqueName ?? "\"\""})";
        }

        public PortCapacity GetDefaultCapacity()
        {
            return PortType == PortType.Data ?
                Direction == Direction.Input ?
                PortCapacity.Single :
                PortCapacity.Multi :
                (PortType == PortType.Execution) ?
                Direction == Direction.Output ?
                PortCapacity.Single :
                PortCapacity.Multi :
                PortCapacity.Multi;
        }

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
                    newTooltip += $" of type {(DataTypeHandle == VSTypeHandle.ThisType ? (NodeModel?.GraphModel)?.FriendlyScriptName : DataTypeHandle.GetMetadata(stencil).FriendlyName)}";
                }

                return newTooltip;
            }
        }

        public void MoveEdgeFirst(IGTFEdgeModel edge)
        {
            if (!HasReorderableEdges)
                return;

            GraphModel.MoveEdgeBefore(edge, ConnectedEdges.First());
        }

        public void MoveEdgeUp(IGTFEdgeModel edge)
        {
            if (!HasReorderableEdges)
                return;

            var edges = ConnectedEdges.ToList();
            var idx = edges.IndexOf(edge);
            if (idx >= 1)
                GraphModel.MoveEdgeBefore(edge, edges[idx - 1]);
        }

        public void MoveEdgeDown(IGTFEdgeModel edge)
        {
            if (!HasReorderableEdges)
                return;

            var edges = ConnectedEdges.ToList();
            var idx = edges.IndexOf(edge);
            if (idx < edges.Count - 1)
                GraphModel.MoveEdgeAfter(edge, edges[idx + 1]);
        }

        public void MoveEdgeLast(IGTFEdgeModel edge)
        {
            if (!HasReorderableEdges)
                return;

            GraphModel.MoveEdgeAfter(edge, ConnectedEdges.Last());
        }
    }
}
