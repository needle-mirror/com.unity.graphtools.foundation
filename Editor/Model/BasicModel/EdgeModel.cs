using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// A model that represents an edge in a graph.
    /// </summary>
    [Serializable]
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
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

        /// <inheritdoc />
        public Vector2 Position
        {
            get => Vector2.zero;
            set => throw new NotImplementedException();
        }

        /// <inheritdoc />
        public virtual IPortModel FromPort
        {
            get => m_FromPortReference.GetPortModel(PortDirection.Output, ref m_FromPortModelCache);
            set
            {
                m_FromPortReference.Assign(value);
                m_FromPortModelCache = value;
            }
        }

        /// <inheritdoc />
        public virtual IPortModel ToPort
        {
            get => m_ToPortReference.GetPortModel(PortDirection.Input, ref m_ToPortModelCache);
            set
            {
                m_ToPortReference.Assign(value);
                m_ToPortModelCache = value;
            }
        }

        /// <inheritdoc />
        public string FromPortId => m_FromPortReference.UniqueId;

        /// <inheritdoc />
        public string ToPortId => m_ToPortReference.UniqueId;

        /// <inheritdoc />
        public SerializableGUID FromNodeGuid => m_FromPortReference.NodeModelGuid;

        /// <inheritdoc />
        public SerializableGUID ToNodeGuid => m_ToPortReference.NodeModelGuid;

        /// <inheritdoc />
        public virtual string EdgeLabel
        {
            get => m_EdgeLabel ?? (FromPort as IHasTitle)?.Title ?? "";
            set => m_EdgeLabel = value;
        }

        /// <inheritdoc />
        public IReadOnlyList<IEdgeControlPointModel> EdgeControlPoints
        {
            get
            {
                if (m_EdgeControlPoints == null)
                    m_EdgeControlPoints = new List<EdgeControlPointModel>();

                return m_EdgeControlPoints;
            }
        }

        /// <inheritdoc />
        public bool EditMode
        {
            get => m_EditMode;
            set => m_EditMode = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EdgeModel"/> class.
        /// </summary>
        public EdgeModel()
        {
            m_Capabilities.AddRange(new[]
            {
                Overdrive.Capabilities.Deletable,
                Overdrive.Capabilities.Copiable,
                Overdrive.Capabilities.Selectable,
                Overdrive.Capabilities.Movable
            });
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public void InsertEdgeControlPoint(int atIndex, Vector2 point, float tightness)
        {
            m_EdgeControlPoints.Insert(atIndex, new EdgeControlPointModel { Position = point, Tightness = tightness });
        }

        /// <inheritdoc />
        public void ModifyEdgeControlPoint(int index, Vector2 point, float tightness)
        {
            tightness = Mathf.Clamp(tightness, 0, 500);
            m_EdgeControlPoints[index].Position = point;
            m_EdgeControlPoints[index].Tightness = tightness;
        }

        /// <inheritdoc />
        public void RemoveEdgeControlPoint(int index)
        {
            m_EdgeControlPoints.RemoveAt(index);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{m_ToPortReference} -> {m_FromPortReference}";
        }

        public void ResetPorts()
        {
            m_FromPortModelCache = default;
            m_ToPortModelCache = default;
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public (PortMigrationResult, PortMigrationResult) AddPlaceHolderPorts(out INodeModel inputNode, out INodeModel outputNode)
        {
            PortMigrationResult inputResult;
            PortMigrationResult outputResult;

            inputNode = outputNode = null;
            if (ToPort == null)
            {
                inputResult = m_ToPortReference.AddPlaceHolderPort(PortDirection.Input) ?
                    PortMigrationResult.PlaceholderPortAdded : PortMigrationResult.PlaceholderPortFailure;

                inputNode = m_ToPortReference.NodeModel;
            }
            else
            {
                inputResult = PortMigrationResult.PlaceholderNotNeeded;
            }

            if (FromPort == null)
            {
                outputResult = m_FromPortReference.AddPlaceHolderPort(PortDirection.Output) ?
                    PortMigrationResult.PlaceholderPortAdded : PortMigrationResult.PlaceholderPortFailure;

                outputNode = m_FromPortReference.NodeModel;
            }
            else
            {
                outputResult = PortMigrationResult.PlaceholderNotNeeded;
            }

            return (inputResult, outputResult);
        }
    }
}
