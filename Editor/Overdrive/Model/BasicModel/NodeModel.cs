using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Serializable]
    public abstract class NodeModel : IInOutPortsNode, IHasTitle, IHasProgress, ICollapsible, ISerializationCallbackReceiver, IGuidUpdate
    {
        [SerializeField, HideInInspector]
        SerializableGUID m_Guid;

        [SerializeField, HideInInspector]
        protected GraphAssetModel m_GraphAssetModel;

        [SerializeField, HideInInspector]
        Vector2 m_Position;

        [SerializeField, HideInInspector]
        Color m_Color;

        [SerializeField, HideInInspector]
        bool m_HasUserColor;

        [SerializeField, HideInInspector]
        string m_Title;

        // Serialize m_InputConstantsById dictionary Keys
        [SerializeField, HideInInspector]
        List<string> m_InputConstantKeys;

        // Serialize m_InputConstantsById dictionary Values
        [SerializeReference]
        protected List<IConstant> m_InputConstants;

        [SerializeField]
        ModelState m_State;

        [SerializeField]
        List<string> m_SerializedCapabilities;

        public GUID Guid
        {
            get
            {
                if (m_Guid.GUID.Empty())
                    AssignNewGuid();
                return m_Guid;
            }
            // Setter for tests only.
            set => m_Guid = value;
        }

        protected List<Capabilities> m_Capabilities;

        public IReadOnlyList<Capabilities> Capabilities => m_Capabilities;

        public virtual IGraphAssetModel AssetModel
        {
            get => m_GraphAssetModel;
            set
            {
                Assert.IsNotNull(value);
                m_GraphAssetModel = (GraphAssetModel)value;
            }
        }

        public virtual IGraphModel GraphModel => AssetModel?.GraphModel;

        protected Stencil Stencil => m_GraphAssetModel != null ? m_GraphAssetModel.GraphModel.Stencil : null;

        public virtual string IconTypeString => "node";

        public virtual string DataTypeString => "";

        public virtual string VariableString => "";

        public ModelState State
        {
            get => m_State;
            set => m_State = value;
        }

        public virtual string Title
        {
            get => m_Title;
            set => m_Title = value;
        }

        public virtual string DisplayTitle => Title.Nicify();

        public virtual string Tooltip { get; set; }

        public Vector2 Position
        {
            get => m_Position;
            set
            {
                if (!this.IsMovable())
                    return;

                m_Position = value;
            }
        }

        public Color Color
        {
            get => m_HasUserColor ? m_Color : Color.clear;
            set
            {
                m_HasUserColor = true;
                m_Color = value;
            }
        }

        public virtual bool AllowSelfConnect => false;

        public bool HasUserColor
        {
            get => m_HasUserColor;
            set => m_HasUserColor = value;
        }

        OrderedPorts m_InputsById;
        OrderedPorts m_OutputsById;
        protected OrderedPorts m_PreviousInputs;
        protected OrderedPorts m_PreviousOutputs;

        Dictionary<string, IConstant> m_InputConstantsById;

        bool m_Collapsed;

        public IReadOnlyDictionary<string, IPortModel> InputsById => m_InputsById;

        public IReadOnlyDictionary<string, IPortModel> OutputsById => m_OutputsById;

        public virtual IReadOnlyList<IPortModel> InputsByDisplayOrder => m_InputsById;

        public virtual IReadOnlyList<IPortModel> OutputsByDisplayOrder => m_OutputsById;

        public IEnumerable<IPortModel> Ports => InputsById.Values.Concat(OutputsById.Values);

        public IReadOnlyDictionary<string, IConstant> InputConstantsById => m_InputConstantsById;

        public bool Collapsed
        {
            get => m_Collapsed;
            set
            {
                if (!this.IsCollapsible())
                    return;

                m_Collapsed = value;
            }
        }

        public virtual bool HasProgress => false;

        public bool Destroyed { get; private set; }

        public NodeModel()
        {
            InternalInitCapabilities();
            m_OutputsById = new OrderedPorts();
            m_InputsById = new OrderedPorts();
            m_Color = new Color(0.776f, 0.443f, 0, 0.5f);
        }

        public void Destroy() => Destroyed = true;

        public virtual void OnConnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel)
        {
        }

        public virtual void OnDisconnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel)
        {
        }

        public void DefineNode()
        {
            OnPreDefineNode();

            m_PreviousInputs = m_InputsById;
            m_PreviousOutputs = m_OutputsById;
            m_InputsById = new OrderedPorts(m_InputsById?.Count ?? 0);
            m_OutputsById = new OrderedPorts(m_OutputsById?.Count ?? 0);

            OnDefineNode();

            RemoveUnusedPorts();
        }

        protected virtual void OnPreDefineNode()
        {
        }

        protected virtual void OnDefineNode()
        {
        }

        void RemoveUnusedPorts()
        {
            foreach (var kv in m_PreviousInputs
                     .Where<KeyValuePair<string, IPortModel>>(kv => !m_InputsById.ContainsKey(kv.Key)))
            {
                DeletePort(kv.Value);
            }

            foreach (var kv in m_PreviousOutputs
                     .Where<KeyValuePair<string, IPortModel>>(kv => !m_OutputsById.ContainsKey(kv.Key)))
            {
                DeletePort(kv.Value);
            }

            if (m_InputConstantsById == null)
            {
                m_InputConstantsById = new Dictionary<string, IConstant>();
                m_InputConstantsById.DeserializeDictionaryFromLists(m_InputConstantKeys, m_InputConstants);
            }

            // remove input constants that aren't used
            var idsToDeletes = m_InputConstantsById
                .Select(kv => kv.Key)
                .Where(id => !m_InputsById.ContainsKey(id)).ToList();
            foreach (var id in idsToDeletes)
            {
                m_InputConstantsById.Remove(id);
            }
        }

        static IPortModel ReuseOrCreatePortModel(IPortModel model, IReadOnlyDictionary<string, IPortModel> previousPorts, OrderedPorts newPorts)
        {
            // reuse existing ports when ids match, otherwise add port
            string id = model.UniqueName;
            IPortModel portModelToAdd = model;
            if (previousPorts.TryGetValue(id, out var existingModel))
            {
                portModelToAdd = existingModel;
                if (portModelToAdd is IHasTitle toAddHasTitle && model is IHasTitle hasTitle)
                    toAddHasTitle.Title = hasTitle.Title;
                portModelToAdd.DataTypeHandle = model.DataTypeHandle;
                portModelToAdd.PortType = model.PortType;
            }
            newPorts.Add(portModelToAdd);
            return portModelToAdd;
        }

        public virtual PortCapacity GetPortCapacity(IPortModel portModel)
        {
            PortCapacity cap = PortCapacity.Single;
            return Stencil?.GetPortCapacity(portModel, out cap) ?? false ? cap : portModel?.GetDefaultCapacity() ?? PortCapacity.Multi;
        }

        // TODO JOCE: This probably need to be a generic interface method that works similar to the CreateNode from GraphModel
        protected virtual IPortModel CreatePort(Direction direction, string portName, PortType portType, TypeHandle dataType, string portId, PortModelOptions options)
        {
            return new PortModel(portName ?? "", portId, options)
            {
                Direction = direction,
                PortType = portType,
                DataTypeHandle = dataType,
                NodeModel = this
            };
        }

        protected void DeletePort(IPortModel portModel, bool removeFromOrderedPorts = false)
        {
            if (GraphModel != null)
            {
                var edgeModels = GraphModel.GetEdgesConnections(portModel);
                GraphModel.DeleteEdges(edgeModels);
            }

            if (removeFromOrderedPorts)
            {
                if (m_InputsById.Remove(portModel))
                {
                    m_PreviousInputs.Remove(portModel);
                }
                else if (m_OutputsById.Remove(portModel))
                {
                    m_PreviousOutputs.Remove(portModel);
                }
            }
        }

        public void AddPlaceHolderPort(Direction direction, string uniqueId)
        {
            if (direction == Direction.Input)
                AddInputPort(uniqueId, PortType.MissingPort, TypeHandle.MissingPort, uniqueId,
                    PortModelOptions.NoEmbeddedConstant);
            else
                AddOutputPort(uniqueId, PortType.MissingPort, TypeHandle.MissingPort, uniqueId,
                    PortModelOptions.NoEmbeddedConstant);
        }

        protected TPortType AddDataInputPort<TPortType>(string portName, TypeHandle typeHandle, string portId = null,
            PortModelOptions options = PortModelOptions.Default, Action<IConstant> preDefine = null) where TPortType : IPortModel
        {
            return (TPortType)AddInputPort(portName, PortType.Data, typeHandle, portId, options, preDefine);
        }

        protected PortModel AddDataInputPort(string portName, TypeHandle typeHandle, string portId = null,
            PortModelOptions options = PortModelOptions.Default, Action<IConstant> preDefine = null)
        {
            return AddDataInputPort<PortModel>(portName, typeHandle, portId, options, preDefine);
        }

        protected PortModel AddDataInputPort<TDataType>(string portName, string portId = null,
            PortModelOptions options = PortModelOptions.Default, TDataType defaultValue = default)
        {
            Action<IConstant> preDefine = null;

            if (defaultValue is Enum || !EqualityComparer<TDataType>.Default.Equals(defaultValue, default))
                preDefine = constantModel => constantModel.ObjectValue = defaultValue;

            return AddDataInputPort(portName, typeof(TDataType).GenerateTypeHandle(), portId, options, preDefine);
        }

        protected TPortType AddDataOutputPort<TPortType>(string portName, TypeHandle typeHandle, string portId = null,
            PortModelOptions options = PortModelOptions.Default) where TPortType : IPortModel
        {
            return (TPortType)AddOutputPort(portName, PortType.Data, typeHandle, portId, options);
        }

        protected PortModel AddDataOutputPort(string portName, TypeHandle typeHandle, string portId = null,
            PortModelOptions options = PortModelOptions.Default)
        {
            return AddDataOutputPort<PortModel>(portName, typeHandle, portId, options);
        }

        protected PortModel AddDataOutputPort<TDataType>(string portName, string portId = null)
        {
            return AddDataOutputPort(portName, typeof(TDataType).GenerateTypeHandle(), portId);
        }

        protected TPortTYpe AddExecutionInputPort<TPortTYpe>(string portName, string portId = null) where TPortTYpe : IPortModel
        {
            return (TPortTYpe)AddInputPort(portName, PortType.Execution, TypeHandle.ExecutionFlow, portId);
        }

        protected PortModel AddExecutionInputPort(string portName, string portId = null)
        {
            return AddExecutionInputPort<PortModel>(portName, portId);
        }

        protected TPortType AddExecutionOutputPort<TPortType>(string portName, string portId = null) where TPortType : IPortModel
        {
            return (TPortType)AddOutputPort(portName, PortType.Execution, TypeHandle.ExecutionFlow, portId);
        }

        protected PortModel AddExecutionOutputPort(string portName, string portId = null)
        {
            return AddExecutionOutputPort<PortModel>(portName, portId);
        }

        protected virtual IPortModel AddInputPort(string portName, PortType portType, TypeHandle dataType, string portId = null, PortModelOptions options = PortModelOptions.Default, Action<IConstant> preDefine = null)
        {
            var portModel = CreatePort(Direction.Input, portName, portType, dataType, portId, options);
            portModel = ReuseOrCreatePortModel(portModel, m_PreviousInputs, m_InputsById);
            UpdateConstantForInput(portModel, preDefine);
            return portModel;
        }

        protected virtual IPortModel AddOutputPort(string portName, PortType portType, TypeHandle dataType, string portId = null, PortModelOptions options = PortModelOptions.Default)
        {
            var portModel = CreatePort(Direction.Output, portName, portType, dataType, portId, options);
            return ReuseOrCreatePortModel(portModel, m_PreviousOutputs, m_OutputsById);
        }

        protected void UpdateConstantForInput(IPortModel inputPort, Action<IConstant> preDefine = null)
        {
            var id = inputPort.UniqueName;
            if ((inputPort.Options & PortModelOptions.NoEmbeddedConstant) != 0)
            {
                m_InputConstantsById?.Remove(id);
                return;
            }

            if (m_InputConstantsById == null)
            {
                m_InputConstantsById = new Dictionary<string, IConstant>();
                m_InputConstantsById.DeserializeDictionaryFromLists(m_InputConstantKeys, m_InputConstants);
            }

            if (m_InputConstantsById.TryGetValue(id, out var constant))
            {
                // Destroy existing constant if not compatible
                Type type = inputPort.DataTypeHandle.Resolve();
                if (!constant.Type.IsAssignableFrom(type))
                {
                    m_InputConstantsById.Remove(id);
                }
            }

            // Create new constant if needed
            if (!m_InputConstantsById.ContainsKey(id)
                && inputPort.CreateEmbeddedValueIfNeeded
                && inputPort.DataTypeHandle != TypeHandle.Unknown
                && Stencil.GetConstantNodeValueType(inputPort.DataTypeHandle) != null)
            {
                var embeddedConstant = ((GraphModel)GraphModel).Stencil.CreateConstantValue(inputPort.DataTypeHandle);
                preDefine?.Invoke(embeddedConstant);
                EditorUtility.SetDirty((Object)AssetModel);
                m_InputConstantsById[id] = embeddedConstant;
            }
        }

        public IConstantNodeModel CloneConstant(IConstantNodeModel source)
        {
            var clone = Activator.CreateInstance(source.GetType());
            EditorUtility.CopySerializedManagedFieldsOnly(source, clone);
            return (IConstantNodeModel)clone;
        }

        public void ReinstantiateInputConstants()
        {
            foreach (var id in m_InputConstantsById.Keys.ToList())
            {
                IConstant inputConstant = m_InputConstantsById[id];
                IConstant newConstant = inputConstant.CloneConstant();
                m_InputConstantsById[id] = newConstant;
                EditorUtility.SetDirty((Object)AssetModel);
            }
        }

        public IPortModel GetPortFitToConnectTo(IPortModel portModel)
        {
            // PF: FIXME: This should be the same as GraphView.GetCompatiblePorts (which will move to GraphModel soon).
            // It should also be coherent with the nodes presented in the searcher.

            var portsToChooseFrom = portModel.Direction == Direction.Input ? OutputsByDisplayOrder : InputsByDisplayOrder;
            return GetFirstPortModelOfType(portModel.PortType, portModel.DataTypeHandle, portsToChooseFrom);
        }

        IPortModel GetFirstPortModelOfType(PortType portType, TypeHandle typeHandle, IReadOnlyList<IPortModel> portModels)
        {
            if (typeHandle != TypeHandle.Unknown && portModels.Any())
            {
                Stencil stencil = portModels.First().GraphModel.Stencil;
                IPortModel unknownPortModel = null;

                // Return the first matching Input portModel
                // If no match was found, return the first Unknown typed portModel
                // Else return null.
                foreach (var portModel in portModels.Where(p => p.PortType == portType))
                {
                    if (portModel.DataTypeHandle == TypeHandle.Unknown && unknownPortModel == null)
                    {
                        unknownPortModel = portModel;
                    }

                    if (typeHandle.IsAssignableFrom(portModel.DataTypeHandle, stencil))
                    {
                        return portModel;
                    }
                }

                if (unknownPortModel != null)
                    return unknownPortModel;
            }

            return null;
        }

        public virtual IEnumerable<IEdgeModel> GetConnectedEdges()
        {
            return NodeModelDefaultImplementations.GetConnectedEdges(this);
        }

        public void AssignNewGuid()
        {
            m_Guid = GUID.Generate();
        }

        void IGuidUpdate.AssignGuid(string guidString)
        {
            m_Guid = new GUID(guidString);
            if (m_Guid.GUID.Empty())
                AssignNewGuid();
        }

        public void Move(Vector2 position)
        {
            if (!this.IsMovable())
                return;

            Position = position;
        }

        public virtual void OnBeforeSerialize()
        {
            m_InputConstantsById.SerializeDictionaryToLists(ref m_InputConstantKeys, ref m_InputConstants);
            m_SerializedCapabilities = m_Capabilities?.Select(c => c.Name).ToList() ?? new List<string>();
        }

        public virtual void OnAfterDeserialize()
        {
            m_OutputsById = new OrderedPorts();
            m_InputsById = new OrderedPorts();

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
                Overdrive.Capabilities.Droppable,
                Overdrive.Capabilities.Copiable,
                Overdrive.Capabilities.Selectable,
                Overdrive.Capabilities.Movable,
                Overdrive.Capabilities.Collapsible
            };
        }
    }
}
