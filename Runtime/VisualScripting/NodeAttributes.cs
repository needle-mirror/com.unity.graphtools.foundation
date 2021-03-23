using System;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace UnityEngine.VisualScripting
{
    [PublicAPI]
    [MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
    public abstract class AbstractNodeAttribute : Attribute
    {
        public Type StencilReferenceType { get; private set; }

        protected AbstractNodeAttribute(Type stencilReferenceType = null)
        {
            if (stencilReferenceType == null)
                return;

            Assert.IsTrue(typeof(IRuntimeStencilReference).IsAssignableFrom(stencilReferenceType));
            StencilReferenceType = stencilReferenceType;
        }
    }

    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = true)]
    public class NodeAttribute : AbstractNodeAttribute
    {
        public NodeAttribute(Type stencilReferenceType = null) : base(stencilReferenceType) { }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TypeMembersNodeAttribute : AbstractNodeAttribute
    {
        public TypeMembersNodeAttribute(Type stencilReferenceType = null) : base(stencilReferenceType) { }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class MethodNodeAttribute : AbstractNodeAttribute
    {
        public MethodNodeAttribute(Type stencilReferenceType = null) : base(stencilReferenceType) { }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class PropertyNodeAttribute : AbstractNodeAttribute
    {
        public PropertyNodeAttribute(Type stencilReferenceType = null) : base(stencilReferenceType) { }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class FieldNodeAttribute : AbstractNodeAttribute
    {
        public FieldNodeAttribute(Type stencilReferenceType = null) : base(stencilReferenceType) { }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ConstantVariableNodeAttribute : AbstractNodeAttribute
    {
        public ConstantVariableNodeAttribute(Type stencilReferenceType = null) : base(stencilReferenceType) { }
    }
}
