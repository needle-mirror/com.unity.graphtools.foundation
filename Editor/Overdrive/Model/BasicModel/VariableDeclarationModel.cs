using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.GraphToolsFoundation.Overdrive;
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

    /// <summary>
    /// A model that represents a variable declaration in a graph.
    /// </summary>
    [Serializable]
    //[MovedFrom(false, "UnityEditor.VisualScripting.Model", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    //[MovedFrom("UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting")]
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class VariableDeclarationModel : DeclarationModel, IVariableDeclarationModel
    {
        [SerializeField]
        TypeHandle m_DataType;
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

        /// <summary>
        /// Get the name of the variable with non-alphanumeric characters replaced by an underscore.
        /// </summary>
        /// <returns>The name of the variable with non-alphanumeric characters replaced by an underscore.</returns>
        public virtual string GetVariableName() => Title.CodifyString();

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

        public string Tooltip
        {
            get => m_Tooltip;
            set => m_Tooltip = value;
        }

        public IConstant InitializationModel
        {
            get => m_InitializationValue;
            set => m_InitializationValue = value;
        }

        public virtual void CreateInitializationValue()
        {
            if (GraphModel.Stencil.GetConstantNodeValueType(DataType) != null)
            {
                InitializationModel = GraphModel.Stencil.CreateConstantValue(DataType);

                EditorUtility.SetDirty((Object)AssetModel);
            }
        }

        bool Equals(VariableDeclarationModel other)
        {
            // ReSharper disable once BaseObjectEqualsIsObjectEquals
            return base.Equals(other) && m_DataType.Equals(other.m_DataType) && m_IsExposed == other.m_IsExposed;
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
                hashCode = (hashCode * 397) ^ m_IsExposed.GetHashCode();
                return hashCode;
            }
        }
    }
}
