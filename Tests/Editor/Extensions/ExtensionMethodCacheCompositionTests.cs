using System;
using NUnit.Framework;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Extensions
{
    class ExtensionTestGraphView : GraphView
    {
        /// <inheritdoc />
        public ExtensionTestGraphView(GraphViewEditorWindow window, CommandDispatcher commandDispatcher, string graphViewName)
            : base(window, commandDispatcher, graphViewName) { }
    }

    class TestModel1 { }
    class TestModel2 { }
    class TestModel3 { }

    [GraphElementsExtensionMethodsCache(typeof(GraphView))]
    static class ExtensionMethods1
    {
        public static IModelUI CreateForPlacemat(this ElementBuilder elementBuilder,
            CommandDispatcher commandDispatcher, IPlacematModel model)
        {
            return GraphViewFactoryExtensions.CreatePlacemat(elementBuilder, commandDispatcher, model);
        }

        public static IModelUI CreateForStickyNote(this ElementBuilder elementBuilder,
            CommandDispatcher commandDispatcher, IStickyNoteModel model)
        {
            return GraphViewFactoryExtensions.CreateStickyNote(elementBuilder, commandDispatcher, model);
        }

        public static IModelUI CreateForTestModel1(this ElementBuilder elementBuilder,
            CommandDispatcher commandDispatcher, TestModel1 model)
        {
            return null;
        }

        public static IModelUI CreateForTestModel2(this ElementBuilder elementBuilder,
            CommandDispatcher commandDispatcher, TestModel2 model)
        {
            return null;
        }
    }

    [GraphElementsExtensionMethodsCache(typeof(ExtensionTestGraphView))]
    static class ExtensionMethods2
    {
        public static IModelUI CreateForStickyNote(this ElementBuilder elementBuilder,
            CommandDispatcher commandDispatcher, IStickyNoteModel model)
        {
            return null;
        }

        public static IModelUI CreateForTestModel2(this ElementBuilder elementBuilder,
            CommandDispatcher commandDispatcher, TestModel2 model)
        {
            return null;
        }

        public static IModelUI CreateForTestModel3(this ElementBuilder elementBuilder,
            CommandDispatcher commandDispatcher, TestModel3 model)
        {
            return null;
        }
    }

    class ExtensionMethodCacheCompositionTests
    {
        [Test]
        [TestCase(typeof(GraphView))]
        [TestCase(typeof(ExtensionTestGraphView))]
        public void TestThatFactoryMethodForINodeModelIsFromDefault(Type domainType)
        {
            var method = ExtensionMethodCache<ElementBuilder>.GetExtensionMethod(domainType,
                typeof(INodeModel), GraphElementFactory.FilterMethods, GraphElementFactory.KeySelector);

            Assert.AreEqual(typeof(GraphViewFactoryExtensions).GetMethod(nameof(GraphViewFactoryExtensions.CreateNode)), method);
        }

        [Test]
        [TestCase(typeof(GraphView))]
        [TestCase(typeof(ExtensionTestGraphView))]
        public void TestThatFactoryMethodForIPlacematModelIsFromEM1(Type domainType)
        {
            var method = ExtensionMethodCache<ElementBuilder>.GetExtensionMethod(domainType,
                typeof(IPlacematModel), GraphElementFactory.FilterMethods, GraphElementFactory.KeySelector);

            Assert.AreEqual(typeof(ExtensionMethods1).GetMethod(nameof(ExtensionMethods1.CreateForPlacemat)), method);
        }

        [Test]
        public void TestThatFactoryMethodForIStickyNoteModelIsFromEM1ForGraphView()
        {
            var method = ExtensionMethodCache<ElementBuilder>.GetExtensionMethod(typeof(GraphView),
                typeof(IStickyNoteModel), GraphElementFactory.FilterMethods, GraphElementFactory.KeySelector);

            Assert.AreEqual(typeof(ExtensionMethods1).GetMethod(nameof(ExtensionMethods1.CreateForStickyNote)), method);
        }

        [Test]
        public void TestThatFactoryMethodForIStickyNoteModelIsFromEM2ForExtensionTestGraphView()
        {
            var method = ExtensionMethodCache<ElementBuilder>.GetExtensionMethod(typeof(ExtensionTestGraphView),
                typeof(IStickyNoteModel), GraphElementFactory.FilterMethods, GraphElementFactory.KeySelector);

            Assert.AreEqual(typeof(ExtensionMethods2).GetMethod(nameof(ExtensionMethods2.CreateForStickyNote)), method);
        }

        [Test]
        [TestCase(typeof(GraphView))]
        [TestCase(typeof(ExtensionTestGraphView))]
        public void TestThatFactoryMethodForModel1IsFromEM1(Type domainType)
        {
            var method = ExtensionMethodCache<ElementBuilder>.GetExtensionMethod(domainType,
                typeof(TestModel1), GraphElementFactory.FilterMethods, GraphElementFactory.KeySelector);

            Assert.AreEqual(typeof(ExtensionMethods1).GetMethod(nameof(ExtensionMethods1.CreateForTestModel1)), method);
        }

        [Test]
        public void TestThatFactoryMethodForModel2IsFromEM2()
        {
            var method = ExtensionMethodCache<ElementBuilder>.GetExtensionMethod(typeof(ExtensionTestGraphView),
                typeof(TestModel2), GraphElementFactory.FilterMethods, GraphElementFactory.KeySelector);

            Assert.AreEqual(typeof(ExtensionMethods2).GetMethod(nameof(ExtensionMethods2.CreateForTestModel2)), method);
        }

        [Test]
        public void TestThatFactoryMethodForModel2IsFromEM1ForGraphView()
        {
            var method = ExtensionMethodCache<ElementBuilder>.GetExtensionMethod(typeof(GraphView),
                typeof(TestModel2), GraphElementFactory.FilterMethods, GraphElementFactory.KeySelector);

            Assert.AreEqual(typeof(ExtensionMethods1).GetMethod(nameof(ExtensionMethods1.CreateForTestModel2)), method);
        }

        [Test]
        public void TestThatFactoryMethodForModel3IsFromEM2()
        {
            var method = ExtensionMethodCache<ElementBuilder>.GetExtensionMethod(typeof(ExtensionTestGraphView),
                typeof(TestModel3), GraphElementFactory.FilterMethods, GraphElementFactory.KeySelector);

            Assert.AreEqual(typeof(ExtensionMethods2).GetMethod(nameof(ExtensionMethods2.CreateForTestModel3)), method);
        }

        [Test]
        public void TestThatFactoryMethodForModel3IsNullForGraphView()
        {
            var method = ExtensionMethodCache<ElementBuilder>.GetExtensionMethod(typeof(GraphView),
                typeof(TestModel3), GraphElementFactory.FilterMethods, GraphElementFactory.KeySelector);

            Assert.IsNull(method);
        }
    }
}
