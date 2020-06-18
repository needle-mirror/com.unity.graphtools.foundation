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
    public class EdgeModel : IEdgeModel, IUndoRedoAware, IGTFEdgeModel
    {
        [Serializable]
        [MovedFrom(false, "UnityEditor.VisualScripting.GraphViewModel", "Unity.GraphTools.Foundation.Overdrive.Editor")]
        internal struct PortReference
        {
            [SerializeField]
            internal SerializableGUID NodeModelGuid;
            [SerializeField]
            GraphAssetModel GraphAssetModel;

            internal INodeModel NodeModel
            {
                get => GraphAssetModel != null && GraphAssetModel.GraphModel.NodesByGuid.TryGetValue(NodeModelGuid, out var node) ? node : null;
                set
                {
                    GraphAssetModel = (GraphAssetModel)value?.AssetModel;
                    NodeModelGuid = value?.Guid ?? default;
                }
            }

            [SerializeField]
            public string UniqueId;

            public void Assign(IPortModel portModel)
            {
                Assert.IsNotNull(portModel);
                NodeModel = portModel.NodeModel;
                UniqueId = portModel.UniqueId;
            }

            public IPortModel GetPortModel(Direction direction, ref IPortModel previousValue)
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

                var nodemodel2 = (nodeModel.VSGraphModel)?.NodesByGuid[nodeModel.Guid];
                if (nodemodel2 != nodeModel)
                {
                    NodeModel = nodemodel2;
                }
                var portModelsByGuid = direction == Direction.Input ? nodeModel.InputsById : nodeModel.OutputsById;
                if (UniqueId != null)
                    portModelsByGuid.TryGetValue(UniqueId, out previousValue);
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

            public static bool TryMigratePorts(ref PortReference portReference, Direction direction, ref IPortModel portModel)
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

        IPortModel m_InputPortModel;
        IPortModel m_OutputPortModel;

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

        public EdgeModel(IGraphModel graphModel, IPortModel inputPort, IPortModel outputPort)
        {
            VSGraphModel = graphModel;
            SetFromPortModels(inputPort, outputPort);
        }

        public ScriptableObject SerializableAsset => m_GraphAssetModel;
        public IGraphAssetModel AssetModel => m_GraphAssetModel;

        public IGraphModel VSGraphModel
        {
            get
            {
                if (m_GraphAssetModel != null)
                    return m_GraphAssetModel.GraphModel;
                return null;
            }
            set => m_GraphAssetModel = value?.AssetModel as GraphAssetModel;
        }

        public IGTFGraphModel GraphModel => VSGraphModel as IGTFGraphModel;

        public void SetFromPortModels(IPortModel newInputPortModel, IPortModel newOutputPortModel)
        {
            m_InputPortReference.Assign(newInputPortModel);
            m_InputPortModel = newInputPortModel;

            m_OutputPortReference.Assign(newOutputPortModel);
            m_OutputPortModel = newOutputPortModel;
        }

        public IPortModel InputPortModel => m_InputPortReference.GetPortModel(Direction.Input, ref m_InputPortModel);
        public IPortModel OutputPortModel => m_OutputPortReference.GetPortModel(Direction.Output, ref m_OutputPortModel);

        public IGTFPortModel ToPort => InputPortModel as IGTFPortModel;
        public IGTFPortModel FromPort => OutputPortModel as IGTFPortModel;

        [SerializeField]
        string m_EdgeLabel;
        public string EdgeLabel
        {
            get => m_EdgeLabel ?? OutputPortModel?.Name;
            set => m_EdgeLabel = value;
        }

        public string GetId()
        {
            return $"{m_InputPortReference}/{m_OutputPortReference}";
        }

        public string OutputId => m_OutputPortReference.UniqueId;

        public string InputId => m_InputPortReference.UniqueId;
        public GUID InputNodeGuid => m_InputPortReference.NodeModelGuid;

        public GUID OutputNodeGuid => m_OutputPortReference.NodeModelGuid;

        public override string ToString()
        {
            return $"{m_InputPortReference} -> {m_OutputPortReference}";
        }

        public void UndoRedoPerformed()
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
            if (InputPortModel == null && !PortReference.TryMigratePorts(ref m_InputPortReference, Direction.Input, ref m_InputPortModel))
                return false;
            if (OutputPortModel == null && !PortReference.TryMigratePorts(ref m_OutputPortReference, Direction.Output, ref m_OutputPortModel))
                return false;
            return true;
        }

        public bool AddPlaceHolderPorts(out INodeModel inputNode, out INodeModel outputNode)
        {
            bool result = true;
            inputNode = outputNode = null;
            if (InputPortModel == null)
            {
                result &= m_InputPortReference.AddPlaceHolderPort(Direction.Input);
                inputNode = m_InputPortReference.NodeModel;
            }

            if (OutputPortModel == null)
            {
                result &= m_OutputPortReference.AddPlaceHolderPort(Direction.Output);
                outputNode = m_OutputPortReference.NodeModel;
            }

            return result;
        }
    }
}
