using System;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Editor;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    [Serializable]
    public class EdgeModel : IEdgeModel, IUndoRedoAware
    {
        [Serializable]
        internal struct PortReference
        {
            [SerializeField]
            internal SerializableGUID NodeModelGuid;
            [SerializeField]
            GraphAssetModel GraphAssetModel;

            INodeModel NodeModel
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

                var nodemodel2 = nodeModel.GraphModel.NodesByGuid[nodeModel.Guid];
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
                return $"{GraphAssetModel?.GetInstanceID()}:{NodeModelGuid}@{UniqueId}";
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

        public EdgeModel(IGraphModel graphModel, IPortModel inputPort, IPortModel outputPort)
        {
            GraphModel = graphModel;
            SetFromPortModels(inputPort, outputPort);
        }

        public ScriptableObject SerializableAsset => m_GraphAssetModel;
        public IGraphAssetModel AssetModel => m_GraphAssetModel;

        public IGraphModel GraphModel
        {
            get => m_GraphAssetModel?.GraphModel;
            set => m_GraphAssetModel = value?.AssetModel as GraphAssetModel;
        }

        // Capabilities
        public CapabilityFlags Capabilities => CapabilityFlags.Selectable | CapabilityFlags.Deletable;

        public void SetFromPortModels(IPortModel newInputPortModel, IPortModel newOutputPortModel)
        {
            m_InputPortReference.Assign(newInputPortModel);
            m_InputPortModel = newInputPortModel;

            m_OutputPortReference.Assign(newOutputPortModel);
            m_OutputPortModel = newOutputPortModel;
        }

        public IPortModel InputPortModel => m_InputPortReference.GetPortModel(Direction.Input, ref m_InputPortModel);
        public IPortModel OutputPortModel => m_OutputPortReference.GetPortModel(Direction.Output, ref m_OutputPortModel);

        [SerializeField]
        string m_EdgeLabel = null;
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
    }
}
