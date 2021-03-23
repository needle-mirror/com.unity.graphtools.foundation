using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.EditorCommon.Extensions;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor.SmartSearch
{
    public class SearcherFilter
    {
        public static SearcherFilter Empty { get; } = new SearcherFilter(SearcherContext.None);

        readonly SearcherContext m_Context;
        readonly Dictionary<string, List<Func<ISearcherItemData, bool>>> m_Filters;

        public SearcherFilter(SearcherContext context)
        {
            m_Context = context;
            m_Filters = new Dictionary<string, List<Func<ISearcherItemData, bool>>>();
        }

        static string GetFilterKey(SearcherContext context, SearcherItemTarget target)
        {
            return $"{context}_{target}";
        }

        public SearcherFilter WithEnums(Stencil stencil)
        {
            this.RegisterType(data => data.Type.GetMetadata(stencil).IsEnum);
            return this;
        }

        public SearcherFilter WithTypesInheriting<T>(Stencil stencil)
        {
            return WithTypesInheriting(stencil, typeof(T));
        }

        public SearcherFilter WithTypesInheriting<T, TA>(Stencil stencil) where TA : Attribute
        {
            return WithTypesInheriting(stencil, typeof(T), typeof(TA));
        }

        public SearcherFilter WithTypesInheriting(Stencil stencil, Type type, Type attributeType = null)
        {
            this.RegisterType(data =>
            {
                var dataType = data.Type.Resolve(stencil);
                return type.IsAssignableFrom(dataType) && (attributeType == null || dataType.GetCustomAttribute(attributeType) != null);
            });
            return this;
        }

        public SearcherFilter WithMacros()
        {
            this.RegisterGraphAsset(data => data.GraphAssetModel != null);
            return this;
        }

        public SearcherFilter WithGraphAsset(IGraphAssetModel assetModel)
        {
            this.RegisterGraphAsset(data => data.GraphAssetModel == assetModel);
            return this;
        }

        public SearcherFilter WithFunctionReferences()
        {
            this.RegisterFunctionRef(data => data.FunctionModel != null);
            return this;
        }

        public SearcherFilter WithVariables(Stencil stencil, IPortModel portModel)
        {
            this.RegisterVariable(data =>
            {
                if (portModel.NodeModel is IOperationValidator operationValidator)
                    return operationValidator.HasValidOperationForInput(portModel, data.Type);

                return portModel.DataType == TypeHandle.Unknown
                || portModel.DataType.IsAssignableFrom(data.Type, stencil);
            });
            return this;
        }

        public SearcherFilter WithVisualScriptingNodes()
        {
            this.RegisterNode(data => data.Type != null);
            return this;
        }

        public SearcherFilter WithVisualScriptingNodes(Type type)
        {
            this.RegisterNode(data => type.IsAssignableFrom(data.Type));
            return this;
        }

        public SearcherFilter WithVisualScriptingNodes(IStackModel stackModel)
        {
            this.RegisterNode(data => data.Type != null && stackModel.AcceptNode(data.Type));
            return this;
        }

        public SearcherFilter WithVisualScriptingNodes(Type type, IStackModel stackModel)
        {
            this.RegisterNode(data => type.IsAssignableFrom(data.Type) && stackModel.AcceptNode(data.Type));
            return this;
        }

        public SearcherFilter WithVisualScriptingNodesExcept(IEnumerable<Type> exceptions)
        {
            this.RegisterNode(data => data.Type != null && !exceptions.Any(e => e.IsAssignableFrom(data.Type)));
            return this;
        }

        public SearcherFilter WithStickyNote()
        {
            this.RegisterStickyNote(data => true);
            return this;
        }

        public SearcherFilter WithEmptyFunction()
        {
            this.RegisterEmptyFunction(data => true);
            return this;
        }

        public SearcherFilter WithStack()
        {
            this.RegisterStack(data => true);
            return this;
        }

        public SearcherFilter WithInlineExpression()
        {
            this.RegisterInlineExpression(data => true);
            return this;
        }

        public SearcherFilter WithBinaryOperators()
        {
            this.RegisterBinaryOperator(data => true);
            return this;
        }

        public SearcherFilter WithBinaryOperators(Type type)
        {
            this.RegisterBinaryOperator(data => TypeSystem.GetOverloadedBinaryOperators(type).Contains(data.Kind));
            return this;
        }

        public SearcherFilter WithUnaryOperators()
        {
            this.RegisterUnaryOperator(data => true);
            return this;
        }

        public SearcherFilter WithUnaryOperators(Type type, bool isConstant = false)
        {
            this.RegisterUnaryOperator(data => !isConstant && TypeSystem.GetOverloadedUnaryOperators(type).Contains(data.Kind));
            return this;
        }

        public SearcherFilter WithControlFlows()
        {
            this.RegisterControlFlow(data => data.Type != null);
            return this;
        }

        public SearcherFilter WithControlFlow(Type type)
        {
            this.RegisterControlFlow(data => data.Type != null && type.IsAssignableFrom(data.Type));
            return this;
        }

        public SearcherFilter WithControlFlows(IStackModel stackModel)
        {
            this.RegisterControlFlow(data => data.Type != null && stackModel.AcceptNode(data.Type));
            return this;
        }

        public SearcherFilter WithControlFlow(Type type, IStackModel stackModel)
        {
            this.RegisterControlFlow(data => data.Type != null
                && type.IsAssignableFrom(data.Type)
                && stackModel.AcceptNode(type));
            return this;
        }

        public SearcherFilter WithIfConditions(TypeHandle inputType, IStackModel stackModel)
        {
            this.RegisterControlFlow(data => data.Type == typeof(IfConditionNodeModel)
                && stackModel.AcceptNode(data.Type)
                && inputType == TypeHandle.Bool
            );

            return this;
        }

        public SearcherFilter WithConstants(Stencil stencil, IPortModel portModel)
        {
            this.RegisterConstant(data =>
            {
                if (portModel.NodeModel is IOperationValidator operationValidator)
                    return operationValidator.HasValidOperationForInput(portModel, data.Type);

                return portModel.DataType == TypeHandle.Unknown
                || portModel.DataType.IsAssignableFrom(data.Type, stencil);
            });
            return this;
        }

        public SearcherFilter WithConstants()
        {
            this.RegisterConstant(data => true);
            return this;
        }

        public SearcherFilter WithMethods(Type declaringType)
        {
            this.RegisterMethod(data =>
            {
                MethodInfo info = data.MethodInfo;

                if (info == null)
                    return false;

                return typeof(Unknown) == declaringType
                || data.MethodInfo.ReflectedType == declaringType
                || data.MethodInfo.IsStatic
                && data.MethodInfo.GetParameters().Any(p => p.ParameterType.IsAssignableFrom(declaringType));
            });

            return this;
        }

        public SearcherFilter WithMethods()
        {
            this.RegisterMethod(data => data.MethodInfo != null);
            return this;
        }

        public SearcherFilter WithMethods(Type declaringType, Type returnType)
        {
            this.RegisterMethod(data =>
            {
                MethodInfo info = data.MethodInfo;

                if (info == null)
                    return false;

                return (typeof(Unknown) == declaringType
                    || data.MethodInfo.ReflectedType == declaringType
                    || data.MethodInfo.IsStatic
                    && data.MethodInfo.GetParameters().Any(p => p.ParameterType.IsAssignableFrom(declaringType)))
                && (returnType == typeof(Unknown) || returnType.IsAssignableFrom(data.MethodInfo.ReturnType));
            });

            return this;
        }

        public SearcherFilter WithProperties(Stencil stencil, IPortModel portModel)
        {
            this.RegisterProperty(data =>
            {
                if (portModel.NodeModel is IOperationValidator operationValidator)
                {
                    TypeHandle propertyType = data.PropertyInfo.PropertyType.GenerateTypeHandle(stencil);
                    return operationValidator.HasValidOperationForInput(portModel, propertyType);
                }

                return portModel.DataType == TypeHandle.Unknown
                || portModel.DataType.IsAssignableFrom(data.PropertyInfo.PropertyType.GenerateTypeHandle(stencil), stencil);
            });
            return this;
        }

        public SearcherFilter WithProperties(Type declaringType, bool allowStaticConstant = true)
        {
            this.RegisterProperty(data =>
            {
                PropertyInfo info = data.PropertyInfo;
                if (info == null || !allowStaticConstant && info.IsStaticConstant())
                    return false;

                return typeof(Unknown) == declaringType || data.PropertyInfo.DeclaringType == declaringType;
            });
            return this;
        }

        public SearcherFilter WithProperties(Type declaringType, Type propertyType, bool allowStaticConstant = true)
        {
            this.RegisterProperty(data =>
            {
                PropertyInfo info = data.PropertyInfo;
                if (info == null || !allowStaticConstant && info.IsStaticConstant())
                    return false;

                return (typeof(Unknown) == declaringType || info.DeclaringType == declaringType)
                && (typeof(Unknown) == propertyType || propertyType.IsAssignableFrom(info.PropertyType));
            });

            return this;
        }

        public SearcherFilter WithProperties()
        {
            this.RegisterProperty(data => data.PropertyInfo != null);
            return this;
        }

        public SearcherFilter WithConstructors()
        {
            this.RegisterConstructor(data => data.ConstructorInfo != null);
            return this;
        }

        public SearcherFilter WithConstantFields(Type fieldType)
        {
            this.RegisterField(data => data.FieldInfo != null
                && (typeof(Unknown) == fieldType || data.FieldInfo.FieldType == fieldType)
                && data.FieldInfo.IsConstantOrStatic());
            return this;
        }

        public SearcherFilter WithConstantFields()
        {
            this.RegisterField(data => data.FieldInfo != null && data.FieldInfo.IsConstantOrStatic());
            return this;
        }

        public SearcherFilter WithFields(Type declaringType)
        {
            this.RegisterField(data => data.FieldInfo != null
                && (typeof(Unknown) == declaringType || data.FieldInfo.ReflectedType == declaringType)
                && !data.FieldInfo.IsConstantOrStatic());
            return this;
        }

        public SearcherFilter WithFields(Type declaringType, Type fieldType)
        {
            this.RegisterField(data => data.FieldInfo != null
                && (typeof(Unknown) == declaringType || data.FieldInfo.ReflectedType == declaringType)
                && (typeof(Unknown) == fieldType || fieldType.IsAssignableFrom(data.FieldInfo.FieldType))
                && !data.FieldInfo.IsConstantOrStatic());
            return this;
        }

        internal void Register<T>(Func<T, bool> filter, SearcherItemTarget target) where T : ISearcherItemData
        {
            bool Func(ISearcherItemData data) => filter.Invoke((T)data);
            var key = GetFilterKey(m_Context, target);

            if (!m_Filters.TryGetValue(key, out var searcherItemsData))
                m_Filters.Add(key, searcherItemsData = new List<Func<ISearcherItemData, bool>>());

            searcherItemsData.Add(Func);
        }

        internal bool ApplyFilters(ISearcherItemData data)
        {
            return m_Filters.TryGetValue(GetFilterKey(m_Context, data.Target), out var filters)
                && filters.Any(f => f.Invoke(data));
        }
    }
}
