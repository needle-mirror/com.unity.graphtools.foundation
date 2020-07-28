using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    class IONodeModel : IInOutPortsNode, IHasTitle, ICollapsible
    {
        public IGTFGraphModel GraphModel { get; set; }

        GUID m_GUID = GUID.Generate();
        public GUID Guid
        {
            get => m_GUID;
            set => m_GUID = value;
        }

        public IGTFGraphAssetModel AssetModel
        {
            get => GraphModel.AssetModel;
            set => GraphModel.AssetModel = value;
        }

        public void AssignNewGuid()
        {
            m_GUID = GUID.Generate();
        }

        public Vector2 Position { get; set; }

        public void Move(Vector2 delta)
        {
            Position += delta;
        }

        public bool IsDeletable => true;
        public bool IsDroppable => true;
        public bool IsCopiable => true;
        public string Title { get; set; }
        public string DisplayTitle => Title;
        public string Tooltip { get; set; }

        public void CreatePorts(int inputPorts, int outputPorts)
        {
            m_InputPorts.Clear();
            for (var i = 0; i < inputPorts; i++)
            {
                m_InputPorts.Add(new PortModel
                {
                    Direction = Direction.Input,
                    NodeModel = this,
                    GraphModel = GraphModel
                });
            }

            m_OutputPorts.Clear();
            for (var i = 0; i < outputPorts; i++)
            {
                m_OutputPorts.Add(new PortModel
                {
                    Direction = Direction.Output,
                    NodeModel = this,
                    GraphModel = GraphModel
                });
            }
        }

        protected List<PortModel> m_InputPorts = new List<PortModel>();
        protected List<PortModel> m_OutputPorts = new List<PortModel>();
        public IEnumerable<IGTFPortModel> InputPorts => m_InputPorts;
        public IEnumerable<IGTFPortModel> OutputPorts => m_OutputPorts;
        public IEnumerable<IGTFPortModel> Ports => InputPorts.Concat(OutputPorts);
        public bool Collapsed { get; set; }
        public Color Color => Color.black;
        public bool AllowSelfConnect => true;
        public bool HasUserColor => false;
        public bool HasProgress => false;
        public string IconTypeString => null;
        public ModelState State => ModelState.Enabled;
        public IReadOnlyDictionary<string, IGTFPortModel> InputsById => InputPorts.ToDictionary(e => e.Guid.ToString());
        public IReadOnlyDictionary<string, IGTFPortModel> OutputsById => OutputPorts.ToDictionary(e => e.Guid.ToString());
        public IReadOnlyList<IGTFPortModel> InputsByDisplayOrder => m_InputPorts;
        public IReadOnlyList<IGTFPortModel> OutputsByDisplayOrder => m_OutputPorts;
        public virtual IEnumerable<IGTFEdgeModel> GetConnectedEdges()
        {
            return NodeModelDefaultImplementations.GetConnectedEdges(this);
        }

        public void DefineNode()
        {
        }

        public void OnConnection(IGTFPortModel selfConnectedPortModel, IGTFPortModel otherConnectedPortModel)
        {
        }

        public void OnDisconnection(IGTFPortModel selfConnectedPortModel, IGTFPortModel otherConnectedPortModel)
        {
        }

        public PortCapacity GetPortCapacity(IGTFPortModel portModel)
        {
            return portModel.GetDefaultCapacity();
        }

        public bool Destroyed { get; private set; }
        public void Destroy() => Destroyed = true;

        public IGTFPortModel GetPortFitToConnectTo(IGTFPortModel portModel)
        {
            var portsToChooseFrom = portModel.Direction == Direction.Input ? OutputsByDisplayOrder : InputsByDisplayOrder;
            return portsToChooseFrom.First(p => p.DataTypeHandle == portModel.DataTypeHandle);
        }

        public void UndoRedoPerformed()
        {
        }
    }
}
