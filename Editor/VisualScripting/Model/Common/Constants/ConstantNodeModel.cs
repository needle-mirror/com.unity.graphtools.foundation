using System;
using JetBrains.Annotations;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model
{
    [Serializable]
    public abstract class ConstantNodeModel : NodeModel, IVariableModel, IConstantNodeModel
    {
        public virtual IPortModel OutputPort { get; protected set; }
        public abstract IVariableDeclarationModel DeclarationModel { get; }
        public abstract object ObjectValue { get; set; }
        public abstract Type Type { get; }
        public abstract bool IsLocked { get; set; }

        public abstract void PredefineSetup(TypeHandle constantTypeHandle);

        public ConstantNodeModel Clone()
        {
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
            if (Type != value.GetType() && !value.GetType().IsSubclassOf(Type))
                throw new ArgumentException($"can't set value of type {value.GetType().Name} in {Type.Name}");
            SetFromOther(value);
        }

        protected abstract void SetFromOther(object o);
    }

    [Serializable]
    public abstract class ConstantNodeModel<TSerialized, TGenerated> : ConstantNodeModel
    {
        [SerializeField]
        bool m_IsLocked;

        [CanBeNull]
        public override IVariableDeclarationModel DeclarationModel => null;

        //TODO decide if this is gonna be a problem in the long term or not
        public TSerialized value;

        protected virtual TSerialized DefaultValue { get; } = default;

        //TODO decide if this is gonna be a problem in the long term or not
        public override Type Type => typeof(TGenerated);
        public override string VariableString => "Constant";
        public override string DataTypeString => Type.FriendlyName();
        public override string Title => string.Empty;

        public override object ObjectValue
        {
            get => value;
            set => this.value = (TSerialized)value;
        }

        public override bool IsLocked
        {
            get => m_IsLocked;
            set => m_IsLocked = value;
        }

        public override void PredefineSetup(TypeHandle constantTypeHandle)
        {
            value = DefaultValue;
        }

        protected override void OnDefineNode()
        {
            OutputPort = AddDataOutputPort(null, typeof(TSerialized).GenerateTypeHandle(Stencil));
        }
    }

    [Serializable]
    public abstract class ConstantNodeModel<T> : ConstantNodeModel<T, T>
    {
        protected override void SetFromOther(object o)
        {
            ObjectValue = o;
        }
    }
}
