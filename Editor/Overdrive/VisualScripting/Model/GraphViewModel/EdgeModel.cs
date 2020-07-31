using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel
{
    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.GraphViewModel", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class EdgeModel : IEdgeModel
    {
        [Serializable]
        [MovedFrom(false, "UnityEditor.VisualScripting.GraphViewModel", "Unity.GraphTools.Foundation.Overdrive.Editor")]
        internal struct PortReference
        {
            [SerializeField]
            internal SerializableGUID NodeModelGuid;
            [SerializeField]
            GraphAssetModel GraphAssetModel;

            internal IGTFNodeModel NodeModel
            {
                get => GraphAssetModel != null && GraphAssetModel.GraphModel.NodesByGuid.TryGetValue(NodeModelGuid, out var node) ? node : null;
                set
                {
                    GraphAssetModel = (GraphAssetModel)value.AssetModel;
                    NodeModelGuid = value.Guid;
                }
            }

            [SerializeField]
            public string UniqueId;

            public void Assign(IGTFPortModel portModel)
            {
                Assert.IsNotNull(portModel);
                NodeModel = portModel.NodeModel;
                UniqueId = portModel.UniqueName;
            }

            public IGTFPortModel GetPortModel(Direction direction, ref IGTFPortModel previousValue)
            {
                var nodeModel = NodeModel;
                if (nodeModel == null)
                {
                    return previousValue = null;
                }

                // when removing a set property member, we patch the edges portIndex
                // the cached value needs to be invalidated
                if (previousValue != null && (previousValue.NodeModel.Guid != nodeModel.Guid || previousValue.Direction != direction))
                {
                    previousValue = null;
                }

                if (previousValue != null)
                    return previousValue;

                previousValue = null;

//                Debug.Log($"OBS {NodeModel} {direction} {UniqueId}");

                var nodemodel2 = nodeModel.GraphModel?.NodesByGuid[nodeModel.Guid];
                if (nodemodel2 != nodeModel)
                {
                    NodeModel = nodemodel2;
                }
                var portModelsByGuid = direction == Direction.Input ? nodeModel.InputsById : nodeModel.OutputsById;
                if (UniqueId != null)
                {
                    if (portModelsByGuid.TryGetValue(UniqueId, out var v))
                        previousValue = v;
                }
                return previousValue;
            }

            public override string ToString()
            {
                if (GraphAssetModel != null)
                {
                    return $"{GraphAssetModel.GetInstanceID()}:{NodeModelGuid}@{UniqueId}";
                }
                return String.Empty;
            }

            public static bool TryMigratePorts(ref PortReference portReference, Direction direction, ref IGTFPortModel portModel)
            {
                if (portReference.NodeModel == null)
                    return false;
                if (portReference.NodeModel is IMigratePorts migratePorts)
                    if (migratePorts.MigratePort(ref portReference.UniqueId, direction))
                    {
                        portModel = null;
                        portReference.GetPortModel(direction, ref portModel);
                        return portModel != null;
                    }

                return false;
            }

            public bool AddPlaceHolderPort(Direction direction)
            {
                if (!(NodeModel is NodeModel n))
                    return false;
                n.AddPlaceHolderPort(direction, UniqueId);
                return true;
            }
        }

        [SerializeField]
        GraphAssetModel m_GraphAssetModel;
        [SerializeField]
        PortReference m_InputPortReference;
        [SerializeField]
        PortReference m_OutputPortReference;

        IGTFPortModel m_InputPortModel;
        IGTFPortModel m_OutputPortModel;

        [SerializeField]
        List<EdgeControlPointModel> m_EdgeControlPoints = new List<EdgeControlPointModel>();

        public ReadOnlyCollection<EdgeControlPointModel> EdgeControlPoints
        {
            get
            {
                if (m_EdgeControlPoints == null)
                    m_EdgeControlPoints = new List<EdgeControlPointModel>();

                return m_EdgeControlPoints.AsReadOnly();
            }
        }

        public void InsertEdgeControlPoint(int atIndex, Vector2 point, float tightness)
        {
            m_EdgeControlPoints.Insert(atIndex, new EdgeControlPointModel { Position = point, Tightness = tightness });
        }

        public void ModifyEdgeControlPoint(int index, Vector2 point, float tightness)
        {
            tightness = Mathf.Clamp(tightness, 0, 500);
            m_EdgeControlPoints[index] = new EdgeControlPointModel { Position = point, Tightness = tightness };
        }

        public void RemoveEdgeControlPoint(int index)
        {
            m_EdgeControlPoints.RemoveAt(index);
        }

        [SerializeField]
        bool m_EditMode;

        public bool EditMode
        {
            get => m_EditMode;
            set => m_EditMode = value;
        }

        public EdgeModel(IGraphModel graphModel, IGTFPortModel inputPort, IGTFPortModel outputPort)
        {
            GraphModel = graphModel;
            SetFromPortModels(inputPort, outputPort);
        }

        public IGTFGraphAssetModel AssetModel => m_GraphAssetModel;

        public IGTFGraphModel GraphModel
        {
            get
            {
                if (m_GraphAssetModel != null)
                    return m_GraphAssetModel.GraphModel;
                return null;
            }
            set => m_GraphAssetModel = value?.AssetModel as GraphAssetModel;
        }

        public void SetFromPortModels(IGTFPortModel newInputPortModel, IGTFPortModel newOutputPortModel)
        {
            m_InputPortReference.Assign(newInputPortModel);
            m_InputPortModel = newInputPortModel;

            m_OutputPortReference.Assign(newOutputPortModel);
            m_OutputPortModel = newOutputPortModel;
        }

        public IGTFPortModel ToPort => m_InputPortReference.GetPortModel(Direction.Input, ref m_InputPortModel);
        public IGTFPortModel FromPort => m_OutputPortReference.GetPortModel(Direction.Output, ref m_OutputPortModel);

        [SerializeField]
        string m_EdgeLabel;
        public string EdgeLabel
        {
            get => m_EdgeLabel ?? (FromPort as IHasTitle)?.Title ?? "";
            set => m_EdgeLabel = value;
        }

        public string GetId()
        {
            return $"{m_InputPortReference}/{m_OutputPortReference}";
        }

        public string FromPortId => m_OutputPortReference.UniqueId;

        public string ToPortId => m_InputPortReference.UniqueId;
        public GUID ToNodeGuid => m_InputPortReference.NodeModelGuid;

        public GUID FromNodeGuid => m_OutputPortReference.NodeModelGuid;

        public override string ToString()
        {
            return $"{m_InputPortReference} -> {m_OutputPortReference}";
        }

        public void ResetPorts()
        {
            m_InputPortModel = default;
            m_OutputPortModel = default;
        }

        public virtual bool IsDeletable => true;

        public Vector2 Position
        {
            get => Vector2.zero;
            set => throw new NotImplementedException();
        }

        public void Move(Vector2 delta)
        {
            for (var i = 0; i < EdgeControlPoints.Count; i++)
            {
                var point = EdgeControlPoints[i];
                ModifyEdgeControlPoint(i, point.Position + delta, point.Tightness);
            }
        }

        public bool IsCopiable => true;

        public bool TryMigratePorts()
        {
            if (ToPort == null && !PortReference.TryMigratePorts(ref m_InputPortReference, Direction.Input, ref m_InputPortModel))
                return false;
            if (FromPort == null && !PortReference.TryMigratePorts(ref m_OutputPortReference, Direction.Output, ref m_OutputPortModel))
                return false;
            return true;
        }

        public bool AddPlaceHolderPorts(out IGTFNodeModel inputNode, out IGTFNodeModel outputNode)
        {
            bool result = true;
            inputNode = outputNode = null;
            if (ToPort == null)
            {
                result &= m_InputPortReference.AddPlaceHolderPort(Direction.Input);
                inputNode = m_InputPortReference.NodeModel;
            }

            if (FromPort == null)
            {
                result &= m_OutputPortReference.AddPlaceHolderPort(Direction.Output);
                outputNode = m_OutputPortReference.NodeModel;
            }

            return result;
        }

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
    }
}
