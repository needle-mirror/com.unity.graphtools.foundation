using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel
{
    [Serializable]
    public abstract class NodeModel : IGTFNodeModel, ISerializationCallbackReceiver, IHasProgress, ICollapsible, IHasIOPorts, IGuidUpdate, IHasTitle
    {
        [SerializeField, HideInInspector, Obsolete("Replaced by a SerializableGUID. Remove after drop 10")]
        string m_GuidAsString = "";

        [SerializeField, HideInInspector]
        SerializableGUID m_Guid;

        [SerializeField, HideInInspector]
        GraphAssetModel m_GraphAssetModel;

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
        [SerializeReference, Obsolete]
        protected List<ConstantNodeModel> m_InputConstantsValues;
        [SerializeReference]
        protected List<IConstant> m_InputConstants;

        [SerializeField]
        ModelState m_State;

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

        protected Stencil Stencil => m_GraphAssetModel != null ? m_GraphAssetModel.GraphModel.Stencil : null;

        public virtual string IconTypeString => "node";

        public virtual string DataTypeString => "";

        public virtual string VariableString => "";

        public IGTFGraphAssetModel AssetModel
        {
            get => m_GraphAssetModel;
            set
            {
                Assert.IsNotNull(value);
                m_GraphAssetModel = (GraphAssetModel)value;
            }
        }

        public IGTFGraphModel GraphModel => AssetModel?.GraphModel;

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

        public Vector2 Position
        {
            get => m_Position;
            set => m_Position = value;
        }

        public Color Color
        {
            get => m_HasUserColor ? m_Color : Color.clear;
            set => m_Color = value;
        }

        // Allows maintaining a ports both by order and by their ids
        // IReadOnlyList<IGTFPortModel> gives access to ports by display order
        // IReadOnlyDictionary<string, IGTFPortModel> gives access to ports by Ids
        [PublicAPI]
        protected class OrderedPorts : IReadOnlyDictionary<string, IGTFPortModel>, IReadOnlyList<IGTFPortModel>
        {
            Dictionary<string, IGTFPortModel> m_Dictionary;
            List<int> m_Order;
            List<IGTFPortModel> m_PortModels;

            public OrderedPorts(int capacity = 0)
            {
                m_Dictionary = new Dictionary<string, IGTFPortModel>(capacity);
                m_Order = new List<int>(capacity);
                m_PortModels = new List<IGTFPortModel>(capacity);
            }

            public void Add(IGTFPortModel portModel)
            {
                m_Dictionary.Add(portModel.UniqueName, portModel);
                m_PortModels.Add(portModel);
                m_Order.Add(m_Order.Count);
            }

            public bool Remove(IGTFPortModel portModel)
            {
                bool found = false;
                if (m_Dictionary.ContainsKey(portModel.UniqueName))
                {
                    m_Dictionary.Remove(portModel.UniqueName);
                    found = true;
                    int index = m_PortModels.FindIndex(x => x == portModel);
                    m_PortModels.Remove(portModel);
                    m_Order.Remove(index);
                    for (int i = 0; i < m_Order.Count; ++i)
                    {
                        if (m_Order[i] > index)
                            --m_Order[i];
                    }
                }
                return found;
            }

            public void SwapPortsOrder(IGTFPortModel a, IGTFPortModel b)
            {
                int indexA = m_PortModels.IndexOf(a);
                int indexB = m_PortModels.IndexOf(b);
                int oldAOrder = m_Order[indexA];
                m_Order[indexA] = m_Order[indexB];
                m_Order[indexB] = oldAOrder;
            }

            #region IReadOnlyDictionary implementation
            public IEnumerator<KeyValuePair<string, IGTFPortModel>> GetEnumerator() => m_Dictionary.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public int Count => m_Dictionary.Count;
            public bool ContainsKey(string key) => m_Dictionary.ContainsKey(key);

            public bool TryGetValue(string key, out IGTFPortModel value)
            {
                return m_Dictionary.TryGetValue(key, out value);
            }

            public IGTFPortModel this[string key] => m_Dictionary[key];

            public IEnumerable<string> Keys => m_Dictionary.Keys;
            public IEnumerable<IGTFPortModel> Values => m_Dictionary.Values;
            #endregion IReadOnlyDictionary implementation

            #region IReadOnlyList<IGTFPortModel> implementation
            IEnumerator<IGTFPortModel> IEnumerable<IGTFPortModel>.GetEnumerator()
            {
                Assert.AreEqual(m_Order.Count, m_PortModels.Count, "these lists are supposed to always be of the same size");
                return m_Order.Select(i => m_PortModels[i]).GetEnumerator();
            }

            public IGTFPortModel this[int index] => m_PortModels[m_Order[index]];
            #endregion IReadOnlyList<IGTFPortModel> implementation
        }

        OrderedPorts m_InputsById;
        OrderedPorts m_OutputsById;
        protected OrderedPorts m_PreviousInputs;
        protected OrderedPorts m_PreviousOutputs;

        Dictionary<string, IConstant> m_InputConstantsById;
        public NodeModel()
        {
            m_OutputsById = new OrderedPorts();
            m_InputsById = new OrderedPorts();
            m_Color = new Color(0.776f, 0.443f, 0, 0.5f);
        }

        public IReadOnlyDictionary<string, IGTFPortModel> InputsById => m_InputsById;
        public IReadOnlyDictionary<string, IGTFPortModel> OutputsById => m_OutputsById;

        public IReadOnlyDictionary<string, IConstant> InputConstantsById => m_InputConstantsById;

        public virtual IReadOnlyList<IGTFPortModel> InputsByDisplayOrder => m_InputsById;

        public virtual IReadOnlyList<IGTFPortModel> OutputsByDisplayOrder => m_OutputsById;

        [Obsolete("Direct indexing dropped, use InputsById or InputsByDisplayOrder instead")]
        public virtual IReadOnlyList<IGTFPortModel> InputPortModels => new List<IGTFPortModel>();
        [Obsolete("Direct indexing dropped, use OutputsById or OutputsByDisplayOrder instead")]
        public virtual IReadOnlyList<IGTFPortModel> OutputPortModels => new List<IGTFPortModel>();

        public virtual bool Collapsed { get; set; }
        public IEnumerable<IGTFPortModel> InputPorts => m_InputsById.Values;
        public IEnumerable<IGTFPortModel> OutputPorts => m_OutputsById.Values;
        public IEnumerable<IGTFPortModel> Ports => InputPorts.Concat(OutputPorts);

        public bool HasUserColor
        {
            get => m_HasUserColor;
            set => m_HasUserColor = value;
        }

        public virtual bool HasProgress => false;

        public virtual void OnConnection(IGTFPortModel selfConnectedPortModel, IGTFPortModel otherConnectedPortModel)
        {
        }

        public virtual void OnDisconnection(IGTFPortModel selfConnectedPortModel, IGTFPortModel otherConnectedPortModel)
        {
        }

        protected virtual void OnPreDefineNode()
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

        void RemoveUnusedPorts()
        {
            foreach (var kv in m_PreviousInputs
                     .Where<KeyValuePair<string, IGTFPortModel>>(kv => !m_InputsById.ContainsKey(kv.Key)))
            {
                DeletePort(kv.Value);
            }

            foreach (var kv in m_PreviousOutputs
                     .Where<KeyValuePair<string, IGTFPortModel>>(kv => !m_OutputsById.ContainsKey(kv.Key)))
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

        static PortModel ReuseOrCreatePortModel(PortModel model, IReadOnlyDictionary<string, IGTFPortModel> previousPorts, OrderedPorts newPorts)
        {
            // reuse existing ports when ids match, otherwise add port
            string id = model.UniqueName;
            PortModel modelToAdd = model;
            if (previousPorts.TryGetValue(id, out var existingModel))
            {
                modelToAdd = (PortModel)existingModel;
                modelToAdd.Title = model.Title;
                modelToAdd.DataTypeHandle = model.DataTypeHandle;
                modelToAdd.PortType = model.PortType;
            }
            newPorts.Add(modelToAdd);
            return modelToAdd;
        }

        protected virtual void OnDefineNode()
        {
        }

        public virtual PortCapacity GetPortCapacity(IGTFPortModel portModel)
        {
            return Stencil.GetPortCapacity(portModel, out var cap) ? cap : portModel?.GetDefaultCapacity() ?? PortCapacity.Multi;
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
            Position = position;
        }

        public void ChangeColor(Color color)
        {
            HasUserColor = true;
            m_Color = color;
        }

        protected virtual PortModel MakePortForNode(Direction direction, string portName, PortType portType, TypeHandle dataType, string portId, PortModel.PortModelOptions options)
        {
            return new PortModel(portName ?? "", portId, options)
            {
                Direction = direction,
                PortType = portType,
                DataTypeHandle = dataType,
                NodeModel = this
            };
        }

        protected PortModel AddDataInputPort<TDataType>(string portName, string portId = null, PortModel.PortModelOptions options = PortModel.PortModelOptions.Default, TDataType defaultValue = default)
        {
            Action<IConstant> preDefine = null;

            if (defaultValue is Enum || !EqualityComparer<TDataType>.Default.Equals(defaultValue, default))
                preDefine = constantModel => constantModel.ObjectValue = defaultValue;

            return AddDataInputPort(portName, typeof(TDataType).GenerateTypeHandle(), portId, options, preDefine);
        }

        protected PortModel AddDataInputPort(string portName, TypeHandle typeHandle, string portId = null, PortModel.PortModelOptions options = PortModel.PortModelOptions.Default, Action<IConstant> preDefine = null)
        {
            return AddInputPort(portName, PortType.Data, typeHandle, portId, options, preDefine);
        }

        protected PortModel AddDataOutputPort<TDataType>(string portName, string portId = null)
        {
            return AddDataOutputPort(portName, typeof(TDataType).GenerateTypeHandle(), portId);
        }

        protected PortModel AddDataOutputPort(string portName, TypeHandle typeHandle, string portId = null, PortModel.PortModelOptions options = PortModel.PortModelOptions.Default)
        {
            return AddOutputPort(portName, PortType.Data, typeHandle, portId, options);
        }

        protected PortModel AddExecutionInputPort(string portName, string portId = null)
        {
            return AddInputPort(portName, PortType.Execution, VSTypeHandle.ExecutionFlow, portId);
        }

        protected PortModel AddExecutionOutputPort(string portName, string portId = null)
        {
            return AddOutputPort(portName, PortType.Execution, VSTypeHandle.ExecutionFlow, portId);
        }

        protected virtual PortModel AddInputPort(string portName, PortType portType, TypeHandle dataType, string portId = null, PortModel.PortModelOptions options = PortModel.PortModelOptions.Default, Action<IConstant> preDefine = null)
        {
            var portModel = MakePortForNode(Direction.Input, portName, portType, dataType, portId, options);
            portModel = ReuseOrCreatePortModel(portModel, m_PreviousInputs, m_InputsById);
            UpdateConstantForInput(portModel, preDefine);
            return portModel;
        }

        protected virtual PortModel AddOutputPort(string portName, PortType portType, TypeHandle dataType, string portId = null, PortModel.PortModelOptions options = PortModel.PortModelOptions.Default)
        {
            var portModel = MakePortForNode(Direction.Output, portName, portType, dataType, portId, options);
            return ReuseOrCreatePortModel(portModel, m_PreviousOutputs, m_OutputsById);
        }

        protected void UpdateConstantForInput(PortModel inputPort, Action<IConstant> preDefine = null)
        {
            var id = inputPort.UniqueName;
            if ((inputPort.Options & PortModel.PortModelOptions.NoEmbeddedConstant) != 0)
            {
                m_InputConstantsById?.Remove(id);
                return;
            }

#pragma warning disable 612
            if (m_InputConstantsValues != null && m_InputConstantsValues.Count != 0 && m_InputConstants?.Count == 0)
            {
                Debug.Log("Migrate embedded constants");
                if (m_InputConstants == null)
                    m_InputConstants = new List<IConstant>();
                for (var i = 0; i < m_InputConstantsValues.Count; i++)
                {
                    var inputConstantsValue = m_InputConstantsValues[i];
                    var newConstant = ((GraphModel)GraphModel).CreateConstantValue(inputConstantsValue.Type.GenerateTypeHandle());
                    newConstant.ObjectValue = inputConstantsValue.ObjectValue;
                    Assert.AreEqual(newConstant.ObjectValue, inputConstantsValue.ObjectValue);
                    m_InputConstants.Add(newConstant);
                }
                m_InputConstantsValues.Clear();
                m_InputConstantsValues = null;
            }
#pragma warning restore 612
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
                var embeddedConstant = ((GraphModel)GraphModel).CreateConstantValue(inputPort.DataTypeHandle, preDefine);
                EditorUtility.SetDirty((Object)AssetModel);
                m_InputConstantsById[id] = embeddedConstant;
            }
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

        protected void DeleteInputPort(PortModel portModel)
        {
            DeletePort(portModel);
        }

        protected void DeleteOutputPort(PortModel portModel)
        {
            DeletePort(portModel);
        }

        protected void DeletePort(IGTFPortModel portModel, bool removeFromOrderedPorts = false)
        {
            var edgeModels = GraphModel.GetEdgesConnections(portModel);
            ((GraphModel)GraphModel).DeleteEdges(edgeModels);
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

        public bool Destroyed { get; private set; }

        public void Destroy() => Destroyed = true;

        public virtual void OnBeforeSerialize()
        {
            m_InputConstantsById.SerializeDictionaryToLists(ref m_InputConstantKeys, ref m_InputConstants);
        }

        public virtual void OnAfterDeserialize()
        {
            m_OutputsById = new OrderedPorts();
            m_InputsById = new OrderedPorts();

            if (m_Guid.GUID.Empty())
            {
#pragma warning disable 618
                if (!String.IsNullOrEmpty(m_GuidAsString))
                {
                    (GraphModel as GraphModel)?.AddGuidToUpdate(this, m_GuidAsString);
                }
#pragma warning restore 618
            }
        }

        public static implicit operator bool(NodeModel n)
        {
            return n != null;
        }

        public virtual bool IsDeletable => true;
        public virtual bool IsDroppable => true;
        public virtual bool IsCopiable => true;

        public void AddPlaceHolderPort(Direction direction, string uniqueId)
        {
            if (direction == Direction.Input)
                AddInputPort(uniqueId, PortType.MissingPort, TypeHandle.MissingPort, uniqueId,
                    PortModel.PortModelOptions.NoEmbeddedConstant);
            else
                AddOutputPort(uniqueId, PortType.MissingPort, TypeHandle.MissingPort, uniqueId,
                    PortModel.PortModelOptions.NoEmbeddedConstant);
        }

        public IGTFPortModel GetPortFitToConnectTo(IGTFPortModel portModel)
        {
            // PF: FIXME: This should be the same as GraphView.GetCompatiblePorts (which will move to GraphModel soon).
            // It should also be coherent with the nodes presented in the searcher.

            var portsToChooseFrom = portModel.Direction == Direction.Input ? OutputsByDisplayOrder : InputsByDisplayOrder;
            return GetFirstPortModelOfType(portModel.PortType, portModel.DataTypeHandle, portsToChooseFrom);
        }

        IGTFPortModel GetFirstPortModelOfType(PortType portType, TypeHandle typeHandle, IReadOnlyList<IGTFPortModel> portModels)
        {
            if (typeHandle != TypeHandle.Unknown && portModels.Any())
            {
                Stencil stencil = portModels.First().GraphModel.Stencil;
                IGTFPortModel unknownPortModel = null;

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
    }
}
