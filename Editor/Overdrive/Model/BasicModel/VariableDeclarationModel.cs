using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting.APIUpdating;
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
    public class VariableDeclarationModel : DeclarationModel, IVariableDeclarationModel
    {
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

        public VariableFlags variableFlags;

        public ModifierFlags Modifiers
        {
            get => (ModifierFlags)m_Modifiers;
            set => m_Modifiers = (int)value;
        }

        public string VariableName
        {
            get => StringExtensions.CodifyString(Title);
            protected set
            {
                if (Title != value)
                    Title = value;
            }
        }

        public VariableType VariableType
        {
            get => m_VariableType;
            protected set => m_VariableType = value;
        }

        public string VariableString => IsExposed ? "Exposed variable" : "Variable";

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
            return m_MetadataModel is T model ? model : default;
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

        [SerializeReference]
        IVariableDeclarationMetadataModel m_MetadataModel;

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

        public static VariableDeclarationModel Create(string variableName, TypeHandle dataType, bool isExposed,
            GraphModel graph, VariableType variableType, ModifierFlags modifierFlags,
            VariableFlags variableFlags = VariableFlags.None, IConstant initializationModel = null,
            SpawnFlags spawnFlags = SpawnFlags.Default, GUID? guid = null)
        {
            Assert.IsNotNull(graph);
            Assert.IsNotNull(graph.AssetModel);

            var decl = new VariableDeclarationModel
            {
                Guid = guid ?? GUID.Generate(),
                AssetModel = graph.AssetModel,
                DataType = dataType,
                VariableName = variableName,
                IsExposed = isExposed,
                VariableType = variableType,
                Modifiers = modifierFlags,
                variableFlags = variableFlags
            };

            if (initializationModel != null)
                decl.InitializationModel = initializationModel;
            else if (!spawnFlags.IsOrphan())
                decl.CreateInitializationValue();

            return decl;
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
    }
}
