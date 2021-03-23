using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    public static class BasicModelReducers
    {
        public static void Register(CommandDispatcher dispatcher)
        {
            dispatcher.RegisterCommandHandler<UpdateModelPropertyValueCommand>(UpdateModelPropertyValueReducer);
        }

        static void UpdateModelPropertyValueReducer(GraphToolState state, UpdateModelPropertyValueCommand command)
        {
            UpdateModelPropertyValueCommand.DefaultCommandHandler(state, command);

            if (command.GraphElementModel is NodeModel nodeModel)
                nodeModel.DefineNode();
        }
    }

    /// <summary>
    /// A model that represents a node in a graph.
    /// </summary>
    [Serializable]
    public abstract class NodeModel : GraphElementModel, IInputOutputPortsNodeModel, IHasTitle, IHasProgress, ICollapsible
    {
        [SerializeField, HideInInspector]
        Vector2 m_Position;

        [SerializeField, HideInInspector]
        string m_Title;

        // for backward compatibility, old way to serialize m_InputConstantsById Keys
        [SerializeField, HideInInspector, Obsolete]
        List<string> m_InputConstantKeys;

        // for backward compatibility, old way to serialize m_InputConstantsById Values
        [SerializeReference, HideInInspector, Obsolete]
        List<IConstant> m_InputConstants;

        [SerializeField, HideInInspector]
        SerializedReferenceDictionary<string, IConstant> m_InputConstantsById;

        [SerializeField]
        ModelState m_State;

        /// <summary>
        /// Stencil for this nodemodel, helper getter for Graphmodel Stencil
        /// </summary>
        protected Stencil Stencil => GraphModel.Stencil;

        public virtual string IconTypeString => "node";

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

        public virtual bool AllowSelfConnect => false;

        OrderedPorts m_InputsById;
        OrderedPorts m_OutputsById;
        protected OrderedPorts m_PreviousInputs;
        protected OrderedPorts m_PreviousOutputs;

        bool m_Collapsed;

        public IReadOnlyDictionary<string, IPortModel> InputsById => m_InputsById;

        public IReadOnlyDictionary<string, IPortModel> OutputsById => m_OutputsById;

        public virtual IReadOnlyList<IPortModel> InputsByDisplayOrder => m_InputsById;

        public virtual IReadOnlyList<IPortModel> OutputsByDisplayOrder => m_OutputsById;

        public IEnumerable<IPortModel> Ports => InputsById.Values.Concat(OutputsById.Values);

        public IReadOnlyDictionary<string, IConstant> InputConstantsById => m_InputConstantsById;

        public virtual bool Collapsed
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

        /// <inheritdoc />
        public override Color DefaultColor => new Color(0.776f, 0.443f, 0, 0.5f);

        public NodeModel()
        {
            InternalInitCapabilities();
            m_OutputsById = new OrderedPorts();
            m_InputsById = new OrderedPorts();
            m_InputConstantsById = new SerializedReferenceDictionary<string, IConstant>();
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

        public void OnCreateNode()
        {
            DefineNode();
        }

        public virtual void OnDuplicateNode(INodeModel sourceNode)
        {
            Title = (sourceNode as IHasTitle)?.Title ?? "";
            DefineNode();
            CloneInputConstants();
        }

        void RemoveUnusedPorts()
        {
            foreach (var kv in m_PreviousInputs
                     .Where<KeyValuePair<string, IPortModel>>(kv => !m_InputsById.ContainsKey(kv.Key)))
            {
                DisconnectPort(kv.Value);
            }

            foreach (var kv in m_PreviousOutputs
                     .Where<KeyValuePair<string, IPortModel>>(kv => !m_OutputsById.ContainsKey(kv.Key)))
            {
                DisconnectPort(kv.Value);
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

        /// <inheritdoc />
        public virtual IPortModel CreatePort(Direction direction, Orientation orientation, string portName, PortType portType,
            TypeHandle dataType, string portId, PortModelOptions options)
        {
            return new PortModel
            {
                Direction = direction,
                Orientation = orientation,
                PortType = portType,
                DataTypeHandle = dataType,
                Title = portName ?? "",
                UniqueName = portId,
                Options = options,
                NodeModel = this,
                AssetModel = AssetModel
            };
        }

        /// <inheritdoc />
        public void DisconnectPort(IPortModel portModel)
        {
#pragma warning disable 618
            DeletePort(portModel, false);
#pragma warning restore 618
        }

        /// <inheritdoc />
        [Obsolete("Use DisconnectPort instead.")]
        public void DeletePort(IPortModel portModel, bool removeFromOrderedPorts = false)
        {
            // TODO JOCE: all known usages have removeFromOrderedPorts = false;
            // In port 0.9 release, remove the parameter from the method so that DeletePort(portModel) always deletes ports.
            if (GraphModel != null)
            {
                var edgeModels = GraphModel.GetEdgesConnections(portModel);
                GraphModel.DeleteEdges(edgeModels.ToList());
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

        /// <inheritdoc />
        public virtual IPortModel AddInputPort(string portName, PortType portType, TypeHandle dataType,
            string portId = null, Orientation orientation = Orientation.Horizontal,
            PortModelOptions options = PortModelOptions.Default, Action<IConstant> initializationCallback = null)
        {
            var portModel = CreatePort(Direction.Input, orientation, portName, portType, dataType, portId, options);
            portModel = ReuseOrCreatePortModel(portModel, m_PreviousInputs, m_InputsById);
            UpdateConstantForInput(portModel, initializationCallback);
            return portModel;
        }

        /// <inheritdoc />
        public virtual IPortModel AddOutputPort(string portName, PortType portType, TypeHandle dataType,
            string portId = null, Orientation orientation = Orientation.Horizontal,
            PortModelOptions options = PortModelOptions.Default)
        {
            var portModel = CreatePort(Direction.Output, orientation, portName, portType, dataType, portId, options);
            return ReuseOrCreatePortModel(portModel, m_PreviousOutputs, m_OutputsById);
        }

        /// <summary>
        /// Updates an input port's constant.
        /// </summary>
        /// <param name="inputPort">The port to update.</param>
        /// <param name="initializationCallback">An initialization method for the constant to be called right after the constant is created.</param>
        protected void UpdateConstantForInput(IPortModel inputPort, Action<IConstant> initializationCallback = null)
        {
            var id = inputPort.UniqueName;
            if ((inputPort.Options & PortModelOptions.NoEmbeddedConstant) != 0)
            {
                m_InputConstantsById.Remove(id);
                return;
            }

            if (m_InputConstantsById.TryGetValue(id, out var constant))
            {
                // Destroy existing constant if not compatible
                var embeddedConstantType = Stencil.GetConstantNodeValueType(inputPort.DataTypeHandle);
                Type portDefinitionType;
                if (embeddedConstantType != null)
                {
                    var instance = (IConstant)Activator.CreateInstance(embeddedConstantType);
                    portDefinitionType = instance.Type;
                }
                else
                {
                    portDefinitionType = inputPort.DataTypeHandle.Resolve();
                }

                if (!constant.Type.IsAssignableFrom(portDefinitionType))
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
                initializationCallback?.Invoke(embeddedConstant);
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

        public void CloneInputConstants()
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

        public void Move(Vector2 delta)
        {
            if (!this.IsMovable())
                return;

            Position += delta;
        }

        /// <inheritdoc />
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            if (Version <= SerializationVersion.GTF_V_0_8_2)
            {
                this.SetCapability(Overdrive.Capabilities.Colorable, true);
            }
            m_OutputsById = new OrderedPorts();
            m_InputsById = new OrderedPorts();
        }

        public virtual void OnAfterDeserializeAssetModel()
        {
#pragma warning disable 612
            // Migrate m_InputConstantKeys and m_InputConstantKeys to m_InputConstantsById
            const string migratedValue = "_ _ _ m_InputConstantKeys was migrated to m_InputConstants _ _ _";

            // If obsolete fields contains data (except special migrated value)
            if (m_InputConstantKeys != null && m_InputConstants != null &&
                m_InputConstantKeys.Count > 0 && m_InputConstants.Count > 0 &&
                m_InputConstantKeys[0] != migratedValue)
            {
                // And if new field is empty, migrate old data in new field.
                // If new field is not empty, we consider that data in new field is supersedes data in obsolete fields.
                if (m_InputConstantsById == null || m_InputConstantsById.Count == 0)
                {
                    m_InputConstantsById =
                        SerializedReferenceDictionary<string, IConstant>.FromLists(m_InputConstantKeys, m_InputConstants);
                }

                // Clear obsolete fields.
                m_InputConstantKeys.Clear();
                m_InputConstantKeys.Add(migratedValue);
                m_InputConstants = null;
            }
#pragma warning restore 612
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
                Overdrive.Capabilities.Droppable,
                Overdrive.Capabilities.Copiable,
                Overdrive.Capabilities.Selectable,
                Overdrive.Capabilities.Movable,
                Overdrive.Capabilities.Collapsible,
                Overdrive.Capabilities.Colorable
            };
        }
    }
}
