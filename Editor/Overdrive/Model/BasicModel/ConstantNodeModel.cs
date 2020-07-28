using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Serializable]
    [MovedFrom("UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting")]
    public sealed class ConstantNodeModel : NodeModel, IGTFConstantNodeModel
    {
        [SerializeField]
        bool m_IsLocked;

        [SerializeReference]
        IConstant m_Value;

        public override string Title => string.Empty;

        public IGTFPortModel OutputPort => NodeModelDefaultImplementations.GetOutputPort(this);

        public IGTFPortModel MainOutputPort => OutputPort;

        public IConstant Value
        {
            get => m_Value;
            set => m_Value = value;
        }

        public object ObjectValue
        {
            get => m_Value.ObjectValue;
            set => m_Value.ObjectValue = value;
        }

        public Type Type => m_Value.Type;

        public bool IsLocked
        {
            get => m_IsLocked;
            set => m_IsLocked = value;
        }

        public void PredefineSetup() =>
            m_Value.ObjectValue = m_Value.DefaultValue;

        public ConstantNodeModel Clone()
        {
            if (GetType() == typeof(ConstantNodeModel))
            {
                return new ConstantNodeModel { Value = Value.CloneConstant() };
            }
            var clone = Activator.CreateInstance(GetType());
            EditorUtility.CopySerializedManagedFieldsOnly(this, clone);
            return (ConstantNodeModel)clone;
        }

        public override string ToString()
        {
            return $"{GetType().Name}: {ObjectValue}";
        }

        public void SetValue<T>(T value)
        {
            if (!(value is Enum) && Type != value.GetType() && !value.GetType().IsSubclassOf(Type))
                throw new ArgumentException($"can't set value of type {value.GetType().Name} in {Type.Name}");
            m_Value.ObjectValue = value;
        }

        protected override void OnDefineNode()
        {
            AddDataOutputPort(null, Value.Type.GenerateTypeHandle());
        }
    }
}
