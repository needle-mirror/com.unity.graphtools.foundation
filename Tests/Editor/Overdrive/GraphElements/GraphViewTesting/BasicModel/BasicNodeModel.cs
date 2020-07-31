using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements.Utilities
{
    public class BasicNodeModel : IGTFNodeModel, IHasIOPorts, IHasTitle, ICollapsible
    {
        public IGTFGraphModel GraphModel { get; set; }

        GUID m_GUID = GUID.Generate();
        public GUID Guid => m_GUID;
        public IGTFGraphAssetModel AssetModel => GraphModel.AssetModel;

        public void AssignNewGuid()
        {
            m_GUID = GUID.Generate();
        }

        public virtual bool IsDeletable => true;
        public bool IsDroppable => true;
        public bool Collapsed { get; set; }

        public Vector2 Position { get; set; }

        public void Move(Vector2 delta)
        {
            Position += delta;
        }

        List<IGTFPortModel> m_InputPorts;
        List<IGTFPortModel> m_OutputPorts;
        public IEnumerable<IGTFPortModel> InputPorts => m_InputPorts;
        public IEnumerable<IGTFPortModel> OutputPorts => m_OutputPorts;
        public IEnumerable<IGTFPortModel> Ports => InputPorts.Concat(OutputPorts);

        public string Title { get; set; }
        public string DisplayTitle => Title;

        static readonly Vector2 k_DefaultSize = new Vector2(200, 100);

        public BasicNodeModel()
            : this("") {}

        public BasicNodeModel(string title, Vector2 position = default)
        {
            GraphModel = null;
            Title = title;
            Position = position;
            m_InputPorts = new List<IGTFPortModel>();
            m_OutputPorts = new List<IGTFPortModel>();
        }

        public BasicPortModel AddPort(Orientation orientation, Direction direction, PortCapacity capacity, Type type)
        {
            if (direction == Direction.Input)
            {
                var basicPortModel = new BasicPortModel(this, direction, orientation, capacity, type);
                m_InputPorts.Add(basicPortModel);
                return basicPortModel;
            }
            if (direction == Direction.Output)
            {
                var basicPortModel = new BasicPortModel(this, direction, orientation, capacity, type);
                m_OutputPorts.Add(basicPortModel);
                return basicPortModel;
            }

            return null;
        }

        public bool IsCopiable => true;
        public Color Color => Color.black;
        public bool HasUserColor => false;
        public bool HasProgress => false;
        public string IconTypeString => null;
        public ModelState State { get; }
        public IReadOnlyDictionary<string, IGTFPortModel> InputsById => m_InputPorts.ToDictionary(e => e.Guid.ToString());
        public IReadOnlyDictionary<string, IGTFPortModel> OutputsById => m_OutputPorts.ToDictionary(e => e.Guid.ToString());
        public IReadOnlyList<IGTFPortModel> InputsByDisplayOrder => m_InputPorts;
        public IReadOnlyList<IGTFPortModel> OutputsByDisplayOrder => m_OutputPorts;
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
