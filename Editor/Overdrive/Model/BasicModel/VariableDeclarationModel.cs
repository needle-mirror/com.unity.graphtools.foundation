using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Flags]
    public enum VariableFlags
    {
        None = 0,
        Generated = 1,
        Hidden = 2,
    }

    [Serializable]
    //[MovedFrom(false, "UnityEditor.VisualScripting.Model", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    [MovedFrom("UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting")]
    public class VariableDeclarationModel : IVariableDeclarationModel, IRenamable, ISerializationCallbackReceiver, IGuidUpdate
    {
        [FormerlySerializedAs("name")]
        [SerializeField]
        string m_Name;

        [SerializeField]
        TypeHandle m_DataType;
        [SerializeField]
        VariableType m_VariableType;
        [SerializeField]
        bool m_IsExposed;
        [SerializeField]
        string m_Tooltip;

        [SerializeReference]
        IConstant m_InitializationValue;

        [SerializeField]
        int m_Modifiers;

        [SerializeField]
        List<string> m_SerializedCapabilities;

        protected List<Capabilities> m_Capabilities;

        public VariableFlags variableFlags;

        public ModifierFlags Modifiers
        {
            get => (ModifierFlags)m_Modifiers;
            set => m_Modifiers = (int)value;
        }

        public string DisplayTitle => Title.Nicify();

        public string Title
        {
            get => m_Name;
            set => m_Name = value;
        }

        public string VariableName
        {
            get => StringExtensions.CodifyString(Title);
            protected set
            {
                if (Title != value)
                    m_Name = value;
            }
        }

        public VariableType VariableType
        {
            get => m_VariableType;
            protected set => m_VariableType = value;
        }

        public string VariableString => IsExposed ? "Exposed variable" : "Variable";
        //public string dataTypeString => (dataType == typeof(ThisType) ? (graphModel)?.friendlyScriptName ?? string.Empty : dataType.FriendlyName());

        public virtual TypeHandle DataType
        {
            get => m_DataType;
            set
            {
                if (m_DataType == value)
                    return;
                m_DataType = value;
                m_InitializationValue = null;
                if (GraphModel.Stencil.RequiresInspectorInitialization(this))
                    CreateInitializationValue();
            }
        }

        public bool IsExposed
        {
            get => m_IsExposed;
            set => m_IsExposed = value;
        }

        public T GetMetadataModel<T>() where T : IVariableDeclarationMetadataModel
        {
            return m_MetadataModel is T ? (T)m_MetadataModel : default;
        }

        public void SetMetadataModel<T>(T value) where T : IVariableDeclarationMetadataModel
        {
            Assert.IsTrue(m_MetadataModel is null || m_MetadataModel is T, "Only one metadata model of can be set on an object. The previous value must be either null or of the same type as the new one");
            m_MetadataModel = value;
        }

        public string Tooltip
        {
            get => m_Tooltip;
            set => m_Tooltip = value;
        }

        [SerializeField]
        GraphAssetModel m_AssetModel;
        public IGraphAssetModel AssetModel
        {
            get => m_AssetModel;
            set => m_AssetModel = (GraphAssetModel)value;
        }

        public IGraphModel GraphModel
        {
            get
            {
                if (m_AssetModel != null)
                    return m_AssetModel.GraphModel;
                return null;
            }

            // TODO JOCE: Get rid of this. Should be done by just setting the asset.
            private set => m_AssetModel = value?.AssetModel as GraphAssetModel;
        }

        // TODO JOCE: Get rid of this. Should be done by just setting the asset.
        public void SetGraphModel(IGraphModel model)
        {
            GraphModel = model;
        }

        [SerializeField]
        SerializableGUID m_Guid;

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
            if (!String.IsNullOrEmpty(m_Id))
            {
                if (GUID.TryParse(m_Id.Replace("-", null), out var migratedGuid))
                    m_Guid = migratedGuid;
                else
                {
                    Debug.Log("FAILED PARSING " + m_Id);
                    m_Guid = GUID.Generate();
                }
            }
            else
                m_Guid = GUID.Generate();
        }

        void IGuidUpdate.AssignGuid(string guidString)
        {
            m_Guid = new GUID(guidString);
            if (m_Guid.GUID.Empty())
                AssignNewGuid();
        }

        public virtual IReadOnlyList<Capabilities> Capabilities => m_Capabilities;

        [SerializeField]
        string m_Id = "";

        [SerializeReference]
        IVariableDeclarationMetadataModel m_MetadataModel;

        public VariableDeclarationModel()
        {
            InternalInitCapabilities();
        }

        public void OnBeforeSerialize()
        {
            m_SerializedCapabilities = m_Capabilities?.Select(c => c.Name).ToList() ?? new List<string>();
        }

        public void OnAfterDeserialize()
        {
            if (m_Guid.GUID.Empty())
            {
                if (!String.IsNullOrEmpty(m_Id))
                {
                    (GraphModel as GraphModel)?.AddGuidToUpdate(this, m_Id);
                }
            }

            if (!m_SerializedCapabilities.Any())
                // If we're reloading an older node
                InitCapabilities();
            else
                m_Capabilities = m_SerializedCapabilities.Select(Overdrive.Capabilities.Get).ToList();
        }

        public void Rename(string newName)
        {
            if (!this.IsRenamable())
                return;

            SetNameFromUserName(newName);
            GraphModel.LastChanges.RequiresRebuild = true;
        }

        public IConstant InitializationModel
        {
            get => m_InitializationValue;
            protected set => m_InitializationValue = value;
        }

        public void CreateInitializationValue()
        {
            if (VariableType == VariableType.EdgePortal)
            {
                InitializationModel = null;
                return;
            }
            if (GraphModel.Stencil.GetConstantNodeValueType(DataType) != null)
            {
                InitializationModel = GraphModel.Stencil.CreateConstantValue(DataType);

                EditorUtility.SetDirty((Object)AssetModel);
            }
        }

        public static T Create<T>(string variableName, TypeHandle dataType, bool isExposed,
            GraphModel graph, VariableType variableType, ModifierFlags modifierFlags,
            VariableFlags variableFlags = VariableFlags.None,
            IConstant initializationModel = null,
            GUID? guid = null) where T : VariableDeclarationModel, new()
        {
            VariableDeclarationModel decl = CreateDeclarationNoUndoRecord<T>(variableName, dataType, isExposed, graph, variableType, modifierFlags,
                variableFlags, initializationModel, guid: guid);
            return (T)decl;
        }

        public static VariableDeclarationModel Create(string variableName, TypeHandle dataType, bool isExposed,
            GraphModel graph, VariableType variableType, ModifierFlags modifierFlags,
            IConstant initializationModel = null, GUID? guid = null)
        {
            return Create<VariableDeclarationModel>(variableName, dataType, isExposed, graph, variableType, modifierFlags, initializationModel: initializationModel, guid: guid);
        }

        public static T CreateDeclarationNoUndoRecord<T>(string variableName, TypeHandle dataType, bool isExposed,
            GraphModel graph, VariableType variableType, ModifierFlags modifierFlags,
            VariableFlags variableFlags,
            IConstant initializationModel = null, SpawnFlags spawnFlags = SpawnFlags.Default, GUID? guid = null) where T : VariableDeclarationModel, new()
        {
            Assert.IsNotNull(graph);
            Assert.IsNotNull(graph.AssetModel);

            var decl = new T();
            decl.m_Guid = guid ?? GUID.Generate();
            SetupDeclaration(variableName, dataType, isExposed, graph, variableType, modifierFlags, variableFlags, decl);
            if (initializationModel != null)
                decl.InitializationModel = initializationModel;
            else if (!spawnFlags.IsOrphan())
                decl.CreateInitializationValue();

            graph.LastChanges.ChangedElements.Add(decl);
            EditorUtility.SetDirty((Object)graph.AssetModel);

            return decl;
        }

        internal static void SetupDeclaration<T>(string variableName, TypeHandle dataType, bool isExposed, GraphModel graph, VariableType variableType, ModifierFlags modifierFlags, VariableFlags variableFlags, T decl) where T : VariableDeclarationModel
        {
            decl.GraphModel = graph;
            decl.DataType = dataType;
            decl.VariableName = variableName;
            decl.IsExposed = isExposed;
            decl.VariableType = variableType;
            decl.Modifiers = modifierFlags;
            decl.variableFlags = variableFlags;
        }

        public static VariableDeclarationModel CreateNoUndoRecord(string variableName, TypeHandle dataType, bool isExposed,
            GraphModel graph, VariableType variableType, ModifierFlags modifierFlags, VariableFlags variableFlags, IConstant initializationModel, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            return CreateDeclarationNoUndoRecord<VariableDeclarationModel>(variableName, dataType, isExposed, graph, variableType, modifierFlags, variableFlags, initializationModel, spawnFlags);
        }

        void SetNameFromUserName(string userName)
        {
            string newName = userName.ToUnityNameFormat();
            if (string.IsNullOrWhiteSpace(newName))
                return;
            VariableName = newName;
        }

        bool Equals(VariableDeclarationModel other)
        {
            // ReSharper disable once BaseObjectEqualsIsObjectEquals
            return base.Equals(other) && m_DataType.Equals(other.m_DataType) && m_VariableType == other.m_VariableType && m_IsExposed == other.m_IsExposed;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((VariableDeclarationModel)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
                int hashCode = base.GetHashCode();
                // ReSharper disable once NonReadonlyMemberInGetHashCode
                hashCode = (hashCode * 397) ^ m_DataType.GetHashCode();
                // ReSharper disable once NonReadonlyMemberInGetHashCode
                hashCode = (hashCode * 397) ^ (int)m_VariableType;
                // ReSharper disable once NonReadonlyMemberInGetHashCode
                hashCode = (hashCode * 397) ^ m_IsExposed.GetHashCode();
                return hashCode;
            }
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
                Overdrive.Capabilities.Renamable,
            };
        }
    }
}
