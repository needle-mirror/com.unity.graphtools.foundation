using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.Model", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class VariableDeclarationModel : IVariableDeclarationModel, IExposeTitleProperty, ISelectable, IDroppable, ICopiable, IDeletable, IModifiable, Overdrive.Model.IRenamable, IGTFGraphElementModel
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
        IConstantNodeModel m_InitializationModel;
        [SerializeField]
        int m_Modifiers;

        public virtual bool IsDeletable => true;

        public virtual bool IsModifiable => true;

        public virtual bool IsRenamable => true;

        public virtual bool IsDroppable => true;

        public VariableFlags variableFlags;

        public ModifierFlags Modifiers
        {
            get => (ModifierFlags)m_Modifiers;
            set => m_Modifiers = (int)value;
        }

        public string Title => Name.Nicify();

        public string Name
        {
            get => m_Name;
            set => m_Name = value;
        }

        public string VariableName
        {
            get => TypeSystem.CodifyString(Name);
            protected set
            {
                if (Name != value)
                    m_Name = ((VSGraphModel)GraphModel).GetUniqueName(value);
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
                (m_InitializationModel as ConstantNodeModel)?.Destroy();
                m_InitializationModel = null;
                if (VSGraphModel.Stencil.RequiresInspectorInitialization(this))
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

        public ScriptableObject SerializableAsset => m_AssetModel;

        [SerializeField]
        GraphAssetModel m_AssetModel;
        public IGraphAssetModel AssetModel => m_AssetModel;

        public GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel.IGraphModel VSGraphModel
        {
            get
            {
                if (m_AssetModel != null)
                    return m_AssetModel.GraphModel;
                return null;
            }

            set => m_AssetModel = value?.AssetModel as GraphAssetModel;
        }

        public IGTFGraphModel GraphModel => VSGraphModel as IGTFGraphModel;

        [SerializeField]
        string m_Id = Guid.NewGuid().ToString();

        [SerializeReference]
        private IVariableDeclarationMetadataModel m_MetadataModel;

        public string GetId()
        {
            return m_Id;
        }

        public IEnumerable<IHasVariableDeclarationModel> FindReferencesInGraph()
        {
            return VSGraphModel.NodeModels.OfType<IHasVariableDeclarationModel>().Where(v => v.DeclarationModel != null && GetId() == v.DeclarationModel.GetId() /* TODO temp until we get rid of node assets ReferenceEquals(v.DeclarationModel, this)*/);
        }

        public void Rename(string newName)
        {
            SetNameFromUserName(newName);
            ((VSGraphModel)GraphModel).LastChanges.RequiresRebuild = true;
        }

        public IHasVariableDeclaration Owner
        {
            get => (IHasVariableDeclaration)GraphModel;
            set => VSGraphModel = (GraphModel)value;
        }

        public IConstantNodeModel InitializationModel
        {
            get => m_InitializationModel;
            protected set => m_InitializationModel = value;
        }

        public void CreateInitializationValue()
        {
            if (VSGraphModel.Stencil.GetConstantNodeModelType(DataType) != null)
            {
                InitializationModel = ((VSGraphModel)GraphModel).CreateConstantNode(
                    Name + "_init",
                    DataType,
                    Vector2.zero,
                    SpawnFlags.Default | SpawnFlags.Orphan);

                Utility.SaveAssetIntoObject(InitializationModel, (Object)AssetModel);
            }
        }

        public static T Create<T>(string variableName, TypeHandle dataType, bool isExposed,
            GraphModel graph, VariableType variableType, ModifierFlags modifierFlags,
            VariableFlags variableFlags = VariableFlags.None,
            IConstantNodeModel initializationModel = null) where T : VariableDeclarationModel, new()
        {
            VariableDeclarationModel decl = CreateDeclarationNoUndoRecord<T>(variableName, dataType, isExposed, graph, variableType, modifierFlags,
                variableFlags, initializationModel);
            return (T)decl;
        }

        public static VariableDeclarationModel Create(string variableName, TypeHandle dataType, bool isExposed,
            GraphModel graph, VariableType variableType, ModifierFlags modifierFlags,
            IConstantNodeModel initializationModel = null)
        {
            return Create<VariableDeclarationModel>(variableName, dataType, isExposed, graph, variableType, modifierFlags, initializationModel: initializationModel);
        }

        public static T CreateDeclarationNoUndoRecord<T>(string variableName, TypeHandle dataType, bool isExposed,
            GraphModel graph, VariableType variableType, ModifierFlags modifierFlags,
            VariableFlags variableFlags,
            IConstantNodeModel initializationModel = null, SpawnFlags spawnFlags = SpawnFlags.Default) where T : VariableDeclarationModel, new()
        {
            Assert.IsNotNull(graph);
            Assert.IsNotNull(graph.AssetModel);

            var decl = new T();
            SetupDeclaration(variableName, dataType, isExposed, graph, variableType, modifierFlags, variableFlags, decl);
            if (initializationModel != null)
                decl.InitializationModel = initializationModel;
            else if (!spawnFlags.IsOrphan())
                decl.CreateInitializationValue();

            if (spawnFlags.IsSerializable())
            {
                ((VSGraphModel)graph).LastChanges.ChangedElements.Add(decl);
                EditorUtility.SetDirty((Object)graph.AssetModel);
            }

            return decl;
        }

        internal static void SetupDeclaration<T>(string variableName, TypeHandle dataType, bool isExposed, GraphModel graph, VariableType variableType, ModifierFlags modifierFlags, VariableFlags variableFlags, T decl) where T : VariableDeclarationModel
        {
            decl.VSGraphModel = graph;
            decl.DataType = dataType;
            decl.VariableName = variableName;
            decl.IsExposed = isExposed;
            decl.VariableType = variableType;
            decl.Modifiers = modifierFlags;
            decl.variableFlags = variableFlags;
        }

        public static VariableDeclarationModel CreateNoUndoRecord(string variableName, TypeHandle dataType, bool isExposed,
            GraphModel graph, VariableType variableType, ModifierFlags modifierFlags, VariableFlags variableFlags, IConstantNodeModel initializationModel, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            return CreateDeclarationNoUndoRecord<VariableDeclarationModel>(variableName, dataType, isExposed, graph, variableType, modifierFlags, variableFlags, initializationModel, spawnFlags);
        }

        public void SetNameFromUserName(string userName)
        {
            string newName = userName.ToUnityNameFormat();
            if (string.IsNullOrWhiteSpace(newName))
                return;

            Undo.RegisterCompleteObjectUndo(SerializableAsset, "Rename Graph Variable");
            VariableName = newName;
        }

        bool Equals(VariableDeclarationModel other)
        {
            return base.Equals(other) && m_DataType.Equals(other.m_DataType) && m_VariableType == other.m_VariableType && m_IsExposed == other.m_IsExposed;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((VariableDeclarationModel)obj);
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ m_DataType.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)m_VariableType;
                hashCode = (hashCode * 397) ^ m_IsExposed.GetHashCode();
                return hashCode;
            }
        }

        public string TitlePropertyName => "m_Name";

        public void UseDeclarationModelCopy(ConstantNodeModel constantModel)
        {
            m_InitializationModel = constantModel.Clone();
        }

        public void AssignNewGuid()
        {
            m_Id = Guid.NewGuid().ToString();
        }

        public bool IsCopiable => true;
    }
}
