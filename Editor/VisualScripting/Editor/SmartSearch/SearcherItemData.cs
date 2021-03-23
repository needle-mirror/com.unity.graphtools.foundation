using System;
using System.Reflection;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;

namespace UnityEditor.VisualScripting.Editor.SmartSearch
{
    public enum SearcherItemTarget
    {
        Constructor,
        Field,
        Method,
        Property,
        Constant,
        ControlFlow,
        UnaryOperator,
        BinaryOperator,
        InlineExpression,
        Stack,
        EmptyFunction,
        StickyNote,
        Node,
        Variable,
        FunctionReference,
        GraphModel,
        Type
    }

    public interface ISearcherItemData
    {
        SearcherItemTarget Target { get; }
    }

    public struct ConstructorSearcherItemData : ISearcherItemData
    {
        public SearcherItemTarget Target => SearcherItemTarget.Constructor;
        public ConstructorInfo ConstructorInfo { get; }

        public ConstructorSearcherItemData(ConstructorInfo constructorInfo)
        {
            ConstructorInfo = constructorInfo;
        }
    }

    public struct FieldSearcherItemData : ISearcherItemData
    {
        public SearcherItemTarget Target => SearcherItemTarget.Field;
        public FieldInfo FieldInfo { get; }

        public FieldSearcherItemData(FieldInfo fieldInfo)
        {
            FieldInfo = fieldInfo;
        }
    }

    public struct MethodSearcherItemData : ISearcherItemData
    {
        public SearcherItemTarget Target => SearcherItemTarget.Method;
        public MethodInfo MethodInfo { get; }

        public MethodSearcherItemData(MethodInfo methodInfo)
        {
            MethodInfo = methodInfo;
        }
    }

    public struct PropertySearcherItemData : ISearcherItemData
    {
        public SearcherItemTarget Target => SearcherItemTarget.Property;
        public PropertyInfo PropertyInfo { get; }

        public PropertySearcherItemData(PropertyInfo propertyInfo)
        {
            PropertyInfo = propertyInfo;
        }
    }

    public struct TypeSearcherItemData : ISearcherItemData
    {
        public SearcherItemTarget Target { get; }
        public TypeHandle Type { get; }

        public TypeSearcherItemData(TypeHandle type, SearcherItemTarget target)
        {
            Type = type;
            Target = target;
        }
    }

    public struct ControlFlowSearcherItemData : ISearcherItemData
    {
        public SearcherItemTarget Target => SearcherItemTarget.ControlFlow;
        public Type Type { get; }

        public ControlFlowSearcherItemData(Type type)
        {
            Type = type;
        }
    }

    public struct UnaryOperatorSearcherItemData : ISearcherItemData
    {
        public SearcherItemTarget Target => SearcherItemTarget.UnaryOperator;
        public UnaryOperatorKind Kind { get; }

        public UnaryOperatorSearcherItemData(UnaryOperatorKind kind)
        {
            Kind = kind;
        }
    }

    public struct BinaryOperatorSearcherItemData : ISearcherItemData
    {
        public SearcherItemTarget Target => SearcherItemTarget.BinaryOperator;
        public BinaryOperatorKind Kind { get; }

        public BinaryOperatorSearcherItemData(BinaryOperatorKind kind)
        {
            Kind = kind;
        }
    }

    public struct NodeSearcherItemData : ISearcherItemData
    {
        public SearcherItemTarget Target => SearcherItemTarget.Node;
        public Type Type { get; }

        public NodeSearcherItemData(Type type)
        {
            Type = type;
        }
    }

    public struct FunctionRefSearcherItemData : ISearcherItemData
    {
        public SearcherItemTarget Target => SearcherItemTarget.FunctionReference;
        public IFunctionModel FunctionModel { get; }
        public IGraphModel GraphModel { get; }

        public FunctionRefSearcherItemData(IGraphModel graphModel, IFunctionModel functionModel)
        {
            FunctionModel = functionModel;
            GraphModel = graphModel;
        }
    }

    public struct GraphAssetSearcherItemData : ISearcherItemData
    {
        public SearcherItemTarget Target => SearcherItemTarget.GraphModel;
        public IGraphAssetModel GraphAssetModel { get; }

        public GraphAssetSearcherItemData(IGraphAssetModel graphAssetModel)
        {
            GraphAssetModel = graphAssetModel;
        }
    }

    public struct SearcherItemData : ISearcherItemData
    {
        public SearcherItemTarget Target { get; }

        public SearcherItemData(SearcherItemTarget target)
        {
            Target = target;
        }
    }
}
