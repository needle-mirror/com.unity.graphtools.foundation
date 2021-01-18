using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Serializable]
    //[MovedFrom(false, "UnityEditor.VisualScripting.GraphViewModel", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    [MovedFrom("UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel")]
    public class EdgeModel : IEditableEdge, ISerializationCallbackReceiver
    {
        [SerializeField]
        GraphAssetModel m_GraphAssetModel;

        [SerializeField, FormerlySerializedAs("m_OutputPortReference")]
        PortReference m_FromPortReference;

        [SerializeField, FormerlySerializedAs("m_InputPortReference")]
        PortReference m_ToPortReference;

        [SerializeField]
        List<EdgeControlPointModel> m_EdgeControlPoints = new List<EdgeControlPointModel>();

        [SerializeField]
        bool m_EditMode;

        [SerializeField]
        protected string m_EdgeLabel;

        [SerializeField]
        SerializableGUID m_Guid;

        [SerializeField]
        List<string> m_SerializedCapabilities;

        protected List<Capabilities> m_Capabilities;

        IPortModel m_FromPortModelCache;

        IPortModel m_ToPortModelCache;

        public IGraphAssetModel AssetModel
        {
            get => m_GraphAssetModel;
            set => m_GraphAssetModel = (GraphAssetModel)value;
        }

        public virtual IGraphModel GraphModel
        {
            get
            {
                if (m_GraphAssetModel != null)
                    return m_GraphAssetModel.GraphModel;
                return null;
            }
        }

        public Vector2 Position
        {
            get => Vector2.zero;
            set => throw new NotImplementedException();
        }

        public virtual IPortModel FromPort
        {
            get => m_FromPortReference.GetPortModel(Direction.Output, ref m_FromPortModelCache);
            set
            {
                m_FromPortReference.Assign(value);
                m_FromPortModelCache = value;
            }
        }

        public virtual IPortModel ToPort
        {
            get => m_ToPortReference.GetPortModel(Direction.Input, ref m_ToPortModelCache);
            set
            {
                m_ToPortReference.Assign(value);
                m_ToPortModelCache = value;
            }
        }

        public string FromPortId => m_FromPortReference.UniqueId;

        public string ToPortId => m_ToPortReference.UniqueId;

        public GUID FromNodeGuid => m_FromPortReference.NodeModelGuid;

        public GUID ToNodeGuid => m_ToPortReference.NodeModelGuid;

        public virtual string EdgeLabel
        {
            get => m_EdgeLabel ?? (FromPort as IHasTitle)?.Title ?? "";
            set => m_EdgeLabel = value;
        }

        public IReadOnlyCollection<IEdgeControlPointModel> EdgeControlPoints
        {
            get
            {
                if (m_EdgeControlPoints == null)
                    m_EdgeControlPoints = new List<EdgeControlPointModel>();

                return m_EdgeControlPoints;
            }
        }

        public bool EditMode
        {
            get => m_EditMode;
            set => m_EditMode = value;
        }

        public virtual IReadOnlyList<Capabilities> Capabilities => m_Capabilities;

        public EdgeModel()
        {
            InternalInitCapabilities();
        }

        public virtual void SetPorts(IPortModel toPortModel, IPortModel fromPortModel)
        {
            FromPort = fromPortModel;
            ToPort = toPortModel;
        }

        public void InsertEdgeControlPoint(int atIndex, Vector2 point, float tightness)
        {
            m_EdgeControlPoints.Insert(atIndex, new EdgeControlPointModel { Position = point, Tightness = tightness });
        }

        public void ModifyEdgeControlPoint(int index, Vector2 point, float tightness)
        {
            tightness = Mathf.Clamp(tightness, 0, 500);
            m_EdgeControlPoints[index].Position = point;
            m_EdgeControlPoints[index].Tightness = tightness;
        }

        public void RemoveEdgeControlPoint(int index)
        {
            m_EdgeControlPoints.RemoveAt(index);
        }

        public override string ToString()
        {
            return $"{m_ToPortReference} -> {m_FromPortReference}";
        }

        public void ResetPorts()
        {
            m_FromPortModelCache = default;
            m_ToPortModelCache = default;
        }

        public void Move(Vector2 delta)
        {
            if (!this.IsMovable())
                return;

            int i = 0;
            foreach (var point in EdgeControlPoints)
            {
                ModifyEdgeControlPoint(i++, point.Position + delta, point.Tightness);
            }
        }

        public bool TryMigratePorts()
        {
            bool result = true;
            if (ToPort == null && !PortReference.TryMigratePorts(ref m_ToPortReference, Direction.Input, ref m_ToPortModelCache))
                result = false;
            if (FromPort == null && !PortReference.TryMigratePorts(ref m_FromPortReference, Direction.Output, ref m_FromPortModelCache))
                result = false;
            return result;
        }

        public bool AddPlaceHolderPorts(out INodeModel inputNode, out INodeModel outputNode)
        {
            bool result = true;
            inputNode = outputNode = null;
            if (ToPort == null)
            {
                result &= m_ToPortReference.AddPlaceHolderPort(Direction.Input);
                inputNode = m_ToPortReference.NodeModel;
            }

            if (FromPort == null)
            {
                result &= m_FromPortReference.AddPlaceHolderPort(Direction.Output);
                outputNode = m_FromPortReference.NodeModel;
            }

            return result;
        }

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

        public void AssignNewGuid()
        {
            m_Guid = GUID.Generate();
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
                Overdrive.Capabilities.Deletable,
                Overdrive.Capabilities.Copiable,
                Overdrive.Capabilities.Selectable,
                Overdrive.Capabilities.Movable
            };
        }
    }
}
