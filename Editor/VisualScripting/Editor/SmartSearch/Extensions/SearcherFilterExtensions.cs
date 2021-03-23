using System;

namespace UnityEditor.VisualScripting.Editor.SmartSearch
{
    public static class SearcherFilterExtensions
    {
        public static void RegisterMethod(this SearcherFilter filter, Func<MethodSearcherItemData, bool> func)
        {
            filter.Register(func, SearcherItemTarget.Method);
        }

        public static void RegisterField(this SearcherFilter filter, Func<FieldSearcherItemData, bool> func)
        {
            filter.Register(func, SearcherItemTarget.Field);
        }

        public static void RegisterProperty(this SearcherFilter filter, Func<PropertySearcherItemData, bool> func)
        {
            filter.Register(func, SearcherItemTarget.Property);
        }

        public static void RegisterConstructor(this SearcherFilter filter, Func<ConstructorSearcherItemData, bool> func)
        {
            filter.Register(func, SearcherItemTarget.Constructor);
        }

        public static void RegisterConstant(this SearcherFilter filter, Func<TypeSearcherItemData, bool> func)
        {
            filter.Register(func, SearcherItemTarget.Constant);
        }

        public static void RegisterControlFlow(this SearcherFilter filter, Func<ControlFlowSearcherItemData, bool> func)
        {
            filter.Register(func, SearcherItemTarget.ControlFlow);
        }

        public static void RegisterUnaryOperator(this SearcherFilter filter, Func<UnaryOperatorSearcherItemData, bool> func)
        {
            filter.Register(func, SearcherItemTarget.UnaryOperator);
        }

        public static void RegisterBinaryOperator(this SearcherFilter filter, Func<BinaryOperatorSearcherItemData, bool> func)
        {
            filter.Register(func, SearcherItemTarget.BinaryOperator);
        }

        public static void RegisterInlineExpression(this SearcherFilter filter, Func<SearcherItemData, bool> func)
        {
            filter.Register(func, SearcherItemTarget.InlineExpression);
        }

        public static void RegisterStack(this SearcherFilter filter, Func<SearcherItemData, bool> func)
        {
            filter.Register(func, SearcherItemTarget.Stack);
        }

        public static void RegisterEmptyFunction(this SearcherFilter filter, Func<SearcherItemData, bool> func)
        {
            filter.Register(func, SearcherItemTarget.EmptyFunction);
        }

        public static void RegisterStickyNote(this SearcherFilter filter, Func<SearcherItemData, bool> func)
        {
            filter.Register(func, SearcherItemTarget.StickyNote);
        }

        public static void RegisterNode(this SearcherFilter filter, Func<NodeSearcherItemData, bool> func)
        {
            filter.Register(func, SearcherItemTarget.Node);
        }

        public static void RegisterVariable(this SearcherFilter filter, Func<TypeSearcherItemData, bool> func)
        {
            filter.Register(func, SearcherItemTarget.Variable);
        }

        public static void RegisterFunctionRef(this SearcherFilter filter, Func<FunctionRefSearcherItemData, bool> func)
        {
            filter.Register(func, SearcherItemTarget.FunctionReference);
        }

        public static void RegisterGraphAsset(this SearcherFilter filter, Func<GraphAssetSearcherItemData, bool> func)
        {
            filter.Register(func, SearcherItemTarget.GraphModel);
        }

        public static void RegisterType(this SearcherFilter filter, Func<TypeSearcherItemData, bool> func)
        {
            filter.Register(func, SearcherItemTarget.Type);
        }
    }
}
