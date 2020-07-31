using System;
using JetBrains.Annotations;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [Serializable]
    public class ConstantNodeModel : NodeModel, IGTFVariableNodeModel, IConstantNodeModel
    {
        [SerializeField]
        bool m_IsLocked;

        [SerializeReference]
        IConstant m_Value;

        public virtual IGTFPortModel MainOutputPort { get; protected set;}

        [CanBeNull]
        public IGTFVariableDeclarationModel VariableDeclarationModel => null;

        public virtual object ObjectValue
        {
            get => m_Value.ObjectValue;
            set => m_Value.ObjectValue = value;
        }

        // TODO @theor remove virtual once we get rid of ConstantNodeModel<T>
        public virtual Type Type => m_Value.Type;

        public bool IsLocked
        {
            get => m_IsLocked;
            set => m_IsLocked = value;
        }

        public virtual void PredefineSetup() =>
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

        public override string Title => string.Empty;

        public override string ToString()
        {
            return $"{GetType().Name}: {ObjectValue}";
        }

        public void SetValue<T>(T value)
        {
            if (!(value is Enum) && Type != value.GetType() && !value.GetType().IsSubclassOf(Type))
                throw new ArgumentException($"can't set value of type {value.GetType().Name} in {Type.Name}");
            SetFromOther(value);
        }

        protected virtual void SetFromOther(object o) => m_Value.ObjectValue = o;
        public IGTFPortModel InputPort => MainOutputPort?.Direction == Direction.Input ? MainOutputPort : null;
        public IGTFPortModel OutputPort => MainOutputPort?.Direction == Direction.Output ? MainOutputPort : null;

        public IConstant Value
        {
            get => m_Value;
            set => m_Value = value;
        }

        protected override void OnDefineNode()
        {
            MainOutputPort = AddDataOutputPort(null, Value.Type.GenerateTypeHandle());
        }
    }

    [Serializable] // should be marked obsolete, but that crashes the serialization
    public abstract class ConstantNodeModel<T> : ConstantNodeModel
    {
        //TODO decide if this is gonna be a problem in the long term or not
        public T value;

        protected virtual T DefaultValue { get; } = default;

        public override Type Type => typeof(T);
        public override string VariableString => "Constant";
        public override string DataTypeString => Type.FriendlyName();

        public override object ObjectValue
        {
            get => value;
            set => this.value = (T)Convert.ChangeType(value, typeof(T));
        }

        public override void PredefineSetup()
        {
            value = DefaultValue;
        }

        protected override void OnDefineNode()
        {
            MainOutputPort = AddDataOutputPort(null, typeof(T).GenerateTypeHandle());
        }

        protected override void SetFromOther(object o)
        {
            ObjectValue = o;
        }
    }
}
