using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.GraphViewModel", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    struct PortReference
    {
        [SerializeField]
        internal SerializableGUID NodeModelGuid;

        [SerializeField]
        GraphAssetModel GraphAssetModel;

        [SerializeField]
        public string UniqueId;

        public INodeModel NodeModel
        {
            get => GraphAssetModel != null && GraphAssetModel.GraphModel.NodesByGuid.TryGetValue(NodeModelGuid, out var node) ? node : null;
            set
            {
                GraphAssetModel = (GraphAssetModel)value.AssetModel;
                NodeModelGuid = value.Guid;
            }
        }

        public void Assign(IPortModel portModel)
        {
            Assert.IsNotNull(portModel);
            NodeModel = portModel.NodeModel;
            UniqueId = portModel.UniqueName;
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

            var nodemodel2 = nodeModel.GraphModel?.NodesByGuid[nodeModel.Guid];
            if (nodemodel2 != nodeModel)
            {
                NodeModel = nodemodel2;
            }

            var portHolder = nodeModel as IInOutPortsNode;
            var portModelsByGuid = direction == Direction.Input ? portHolder?.InputsById : portHolder?.OutputsById;
            if (portModelsByGuid != null && UniqueId != null)
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
}
