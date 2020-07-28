using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    class SingleInputNodeModel : ISingleInputPortNode, IHasTitle, ICollapsible
    {
        public IGTFGraphModel GraphModel => null;

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

        PortModel m_Port = new PortModel { Direction = Direction.Input };
        public IGTFPortModel InputPort => m_Port;

        public IEnumerable<IGTFPortModel> Ports
        {
            get
            {
                yield return InputPort;
            }
        }

        public string Title { get; set; }
        public string DisplayTitle => Title;
        public string Tooltip { get; set; }
        public bool Collapsed { get; set; }
        public Color Color => Color.black;
        public bool AllowSelfConnect => true;
        public bool HasUserColor => false;
        public bool HasProgress => false;
        public string IconTypeString => null;
        public ModelState State => ModelState.Enabled;
        public IReadOnlyDictionary<string, IGTFPortModel> InputsById => Ports.ToDictionary(e => e.Guid.ToString());
        public IReadOnlyDictionary<string, IGTFPortModel> OutputsById => new Dictionary<string, IGTFPortModel>();
        public IReadOnlyList<IGTFPortModel> InputsByDisplayOrder => Ports.ToList();
        public IReadOnlyList<IGTFPortModel> OutputsByDisplayOrder => new IGTFPortModel[0];
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
    }
}
