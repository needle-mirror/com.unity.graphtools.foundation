using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// A model that represents an edge in a graph.
    /// </summary>
    [Serializable]
    //[MovedFrom(false, "UnityEditor.VisualScripting.GraphViewModel", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    [MovedFrom("UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel")]
    public class EdgeModel : GraphElementModel, IEditableEdge
    {
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

        IPortModel m_FromPortModelCache;

        IPortModel m_ToPortModelCache;

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

        /// <summary>
        /// The unique identifier of the input node of the edge.
        /// </summary>
        public SerializableGUID FromNodeGuid => m_FromPortReference.NodeModelGuid;

        /// <summary>
        /// The unique identifier of the output node of the edge.
        /// </summary>
        public SerializableGUID ToNodeGuid => m_ToPortReference.NodeModelGuid;

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

        public EdgeModel()
        {
            InternalInitCapabilities();
        }

        public virtual void SetPorts(IPortModel toPortModel, IPortModel fromPortModel)
        {
            Assert.IsNotNull(toPortModel);
            Assert.IsNotNull(toPortModel.NodeModel);
            Assert.IsNotNull(fromPortModel);
            Assert.IsNotNull(fromPortModel.NodeModel);

            FromPort = fromPortModel;
            ToPort = toPortModel;

            toPortModel.NodeModel.OnConnection(toPortModel, fromPortModel);
            fromPortModel.NodeModel.OnConnection(fromPortModel, toPortModel);
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

        public (PortMigrationResult, PortMigrationResult) TryMigratePorts(out INodeModel inputNode, out INodeModel outputNode)
        {
            bool addPlaceholderPort = false;

            inputNode = outputNode = null;

            if (ToPort == null &&
                !PortReference.TryMigratePorts(ref m_ToPortReference, Direction.Input, ref m_ToPortModelCache))
                addPlaceholderPort = true;

            if (FromPort == null &&
                !PortReference.TryMigratePorts(ref m_FromPortReference, Direction.Output, ref m_FromPortModelCache))
                addPlaceholderPort = true;

            if (addPlaceholderPort)
            {
                return AddPlaceHolderPorts(out inputNode, out outputNode);
            }

            return (PortMigrationResult.PlaceholderNotNeeded, PortMigrationResult.PlaceholderNotNeeded);
        }

        (PortMigrationResult, PortMigrationResult) AddPlaceHolderPorts(out INodeModel inputNode, out INodeModel outputNode)
        {
            PortMigrationResult inputResult = PortMigrationResult.None;
            PortMigrationResult outputResult = PortMigrationResult.None;

            inputNode = outputNode = null;
            if (ToPort == null)
            {
                inputResult = m_ToPortReference.AddPlaceHolderPort(Direction.Input) ?
                    PortMigrationResult.PlaceholderPortAdded : PortMigrationResult.PlaceholderPortFailure;

                inputNode = m_ToPortReference.NodeModel;
            }
            else
            {
                inputResult = PortMigrationResult.PlaceholderNotNeeded;
            }

            if (FromPort == null)
            {
                outputResult = m_FromPortReference.AddPlaceHolderPort(Direction.Output) ?
                    PortMigrationResult.PlaceholderPortAdded : PortMigrationResult.PlaceholderPortFailure;

                outputNode = m_FromPortReference.NodeModel;
            }
            else
            {
                outputResult = PortMigrationResult.PlaceholderNotNeeded;
            }

            return (inputResult, outputResult);
        }

        /// <inheritdoc />
        protected override void InitCapabilities()
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
