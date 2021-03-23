using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using Object = System.Object;
using Port = UnityEditor.Experimental.GraphView.Port;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    public class PortModel : IPortModel
    {
        [Flags]
        public enum PortModelOptions
        {
            None = 0,
            NoEmbeddedConstant = 1,
            Default = None,
        }

        public static bool Equivalent(IPortModel a, IPortModel b)
        {
            if (a == null || b == null)
                return a == b;
            return a.Direction == b.Direction && a.NodeModel.Guid == b.NodeModel.Guid && a.UniqueId == b.UniqueId;
        }

        public PortModelOptions Options { get; protected set; }

        TypeHandle m_DataType;

        public PortModel(string name = null, string uniqueId = null, PortModelOptions options = PortModelOptions.Default)
        {
            m_Name = name;
            m_UniqueId = uniqueId;
            Options = options;
        }

        public ScriptableObject SerializableAsset => (ScriptableObject)NodeModel.GraphModel.AssetModel;
        public IGraphAssetModel AssetModel => GraphModel?.AssetModel;
        public IGraphModel GraphModel => NodeModel?.GraphModel;

        string m_Name;

        string m_UniqueId;

        public string UniqueId => m_UniqueId ?? m_Name ?? "";

        public string Name
        {
            get => m_Name;
            set
            {
                if (value == m_Name)
                    return;
                m_Name = value;
                OnValueChanged?.Invoke();
            }
        }

        INodeModel m_NodeModel;

        public INodeModel NodeModel
        {
            get => m_NodeModel;
            set
            {
                if (value == m_NodeModel)
                    return;

                m_NodeModel = value;
                OnValueChanged?.Invoke();
            }
        }

        public ConstantNodeModel EmbeddedValue
        {
            get
            {
                if (NodeModel is NodeModel node && node.InputConstantsById.TryGetValue(UniqueId, out var inputModel))
                {
                    return inputModel;
                }

                return null;
            }
        }

        public Action<IChangeEvent, Store, IPortModel> EmbeddedValueEditorValueChangedOverride { get; set; }

        public virtual bool CreateEmbeddedValueIfNeeded => PortType == PortType.Data;

        public IEnumerable<IPortModel> ConnectionPortModels
        {
            get { Assert.IsNotNull(GraphModel, $"portModel {Name} has a null GraphModel reference"); return GraphModel.GetConnections(this); }
        }

        PortType m_PortType;

        public PortType PortType
        {
            get => m_PortType;
            set
            {
                if (value == m_PortType)
                    return;

                m_PortType = value;
                OnValueChanged?.Invoke();
            }
        }

        Direction m_Direction;

        public Direction Direction
        {
            get => m_Direction;
            set
            {
                if (value == m_Direction)
                    return;

                m_Direction = value;
                OnValueChanged?.Invoke();
            }
        }

        // Give node model priority over self
        public Port.Capacity Capacity => NodeModel?.GetPortCapacity(this) ?? GetDefaultCapacity();

        public bool Connected => ConnectionPortModels.Any();

        public Action OnValueChanged { get; set; }

        // Capabilities
        public CapabilityFlags Capabilities => 0;

        public TypeHandle DataType
        {
            get
            {
                return m_DataType;
            }
            set
            {
                if (value == m_DataType)
                    return;

                m_DataType = value;
                OnValueChanged?.Invoke();
            }
        }

        public override string ToString()
        {
            return $"Port {NodeModel}: {PortType} {Name}(id: {UniqueId ?? "\"\""})";
        }

        public Port.Capacity GetDefaultCapacity()
        {
            return (PortType == PortType.Data || PortType == PortType.Instance) ?
                Direction == Direction.Input ?
                Port.Capacity.Single :
                Port.Capacity.Multi :
                (PortType == PortType.Execution || PortType == PortType.Loop) ?
                Direction == Direction.Output ?
                Port.Capacity.Single :
                Port.Capacity.Multi :
                Port.Capacity.Multi;
        }

        public string GetId()
        {
            return string.Empty;
        }

        public string IconTypeString
        {
            get
            {
                Stencil stencil = NodeModel.GraphModel.Stencil;

                // TODO: should TypHandle.Resolve do this for us?
                // @THEOR SAID HE WOULD THINK ABOUT IT (written on CAPS DAY 2018)
                if (NodeModel is EnumConstantNodeModel enumConst)
                {
                    Type t = enumConst.EnumType.Resolve(stencil);
                    return "type" + t.Name;
                }

                Type thisPortType = DataType.Resolve(stencil);

                if (thisPortType.IsSubclassOf(typeof(Component)))
                    return "typeComponent";
                if (thisPortType.IsSubclassOf(typeof(GameObject)))
                    return "typeGameObject";
                if (thisPortType.IsSubclassOf(typeof(Rigidbody)) || thisPortType.IsSubclassOf(typeof(Rigidbody2D)))
                    return "typeRigidBody";
                if (thisPortType.IsSubclassOf(typeof(Transform)))
                    return "typeTransform";
                if (thisPortType.IsSubclassOf(typeof(Texture)) || thisPortType.IsSubclassOf(typeof(Texture2D)))
                    return "typeTexture2D";
                if (thisPortType.IsSubclassOf(typeof(KeyCode)))
                    return "typeKeycode";
                if (thisPortType.IsSubclassOf(typeof(Material)))
                    return "typeMaterial";
                if (thisPortType == typeof(Object))
                    return "typeObject";
                return "type" + thisPortType.Name;
            }
        }

        public virtual string ToolTip
        {
            get
            {
                string newTooltip = Direction == Direction.Output ? "Output" : "Input";
                switch (PortType)
                {
                    case PortType.Execution:
                        newTooltip += " execution flow";
                        if (NodeModel.IsCondition)
                            newTooltip += $" ({Name.ToLower()} condition)";
                        break;
                    case PortType.Loop:
                        newTooltip += " loop";
                        break;
                    case PortType.Data:
                    case PortType.Instance:
                        var stencil = GraphModel.Stencil;
                        newTooltip += $" of type {(DataType == TypeHandle.ThisType ? (NodeModel?.GraphModel)?.FriendlyScriptName : DataType.GetMetadata(stencil).FriendlyName)}";
                        break;
                    case PortType.Event:
                        newTooltip += " event";
                        break;
                }
                return newTooltip;
            }
        }
    }
}
