using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Moq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.SmartSearch
{
    class SearcherFilterTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;

        internal abstract class BaseFakeObject {}

#pragma warning disable CS0414
#pragma warning disable CS0649
        [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
        internal class FakeObject : BaseFakeObject
        {
            public static readonly int S = 1;
            public const int I = 0;
            public readonly FakeObject Child;
            public int Index = 1;
            public static int Max = 10;

            public string Name { get; }
            public static int Zero => 0;

            [UsedImplicitly]
            public FakeObject() {}

            public void Foo() {}
            public FakeObject GetChild() { return Child; }

            public static bool operator!(FakeObject fo) { return false; }
        }
#pragma warning restore CS0649
#pragma warning restore CS0414

        [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
        internal class OtherFakeObject
        {
            public static void Bar(FakeObject fo) {}

            public void DoStuff() {}
        }

        [UsedImplicitly]
        static IEnumerable<TestCaseData> WithGraphAssetsTestCaseData
        {
            get
            {
                yield return new TestCaseData(new Mock<IGraphAssetModel>().Object, true);
                yield return new TestCaseData(null, false);
            }
        }

        [UsedImplicitly]
        static IEnumerable<TestCaseData> WithMethodsTestCaseData
        {
            get
            {
                yield return new TestCaseData(new Mock<MethodInfo>().Object, true);
                yield return new TestCaseData(null, false);
            }
        }

        [TestCaseSource(nameof(WithGraphAssetsTestCaseData))]
        public void TestWithGraphAssets(IGraphAssetModel graphAssetModel, bool result)
        {
            var filter = new SearcherFilter(SearcherContext.Graph).WithMacros();
            var data = new GraphAssetSearcherItemData(graphAssetModel);

            Assert.AreEqual(result, filter.ApplyFilters(data));
        }

        [TestCase(typeof(Unknown), typeof(string), true)]
        [TestCase(typeof(Transform), typeof(Component), false)]
        [TestCase(typeof(Component), typeof(Transform), true)]
        public void TestWithVariables(Type portDataType, Type variableType, bool result)
        {
            var portMock = new Mock<IPortModel>();
            portMock.Setup(p => p.DataTypeHandle).Returns(Stencil.GenerateTypeHandle(portDataType));
            portMock.Setup(p => p.NodeModel).Returns(new Mock<INodeModel>().Object);

            var filter = new SearcherFilter(SearcherContext.Graph).WithVariables(Stencil, portMock.Object);
            var data = new TypeSearcherItemData(Stencil.GenerateTypeHandle(variableType));


            Assert.AreEqual(result, filter.ApplyFilters(data));
        }

        [TestCase(typeof(ThisNodeModel), typeof(ThisNodeModel), false)]
        [TestCase(typeof(int), typeof(string), true)]
        [TestCase(typeof(string), typeof(int), true)]
        [TestCase(typeof(string), null, false)]
        public void TestWithVisualScriptingNodesExcept(Type exception, Type dataType, bool result)
        {
            var filter = new SearcherFilter(SearcherContext.Graph).WithVisualScriptingNodesExcept(new[] { exception });
            var data = new NodeSearcherItemData(dataType);

            Assert.AreEqual(result, filter.ApplyFilters(data));
        }

        [Test]
        public void TestWithStickyNote()
        {
            var filter = new SearcherFilter(SearcherContext.Graph).WithStickyNote();
            var data = new TagSearcherItemData(CommonSearcherTags.StickyNote);

            Assert.IsTrue(filter.ApplyFilters(data));
        }

        [TestCase(typeof(string), typeof(string), true)]
        [TestCase(typeof(string), typeof(object), true)]
        [TestCase(typeof(object), typeof(string), false)]
        [TestCase(typeof(int), typeof(string), false)]
        [TestCase(typeof(int), typeof(Unknown), true)]
        public void TestWithConstantsOfType(Type constType, Type portDataType, bool result)
        {
            var portMock = new Mock<IPortModel>();
            portMock.Setup(p => p.DataTypeHandle).Returns(Stencil.GenerateTypeHandle(portDataType));

            var filter = new SearcherFilter(SearcherContext.Graph).WithConstants(Stencil, portMock.Object);
            var data = TypeSearcherItemData.Constant(Stencil.GenerateTypeHandle(constType));

            Assert.AreEqual(result, filter.ApplyFilters(data));
        }

        [Test]
        public void TestWithConstants()
        {
            var filter = new SearcherFilter(SearcherContext.Graph).WithConstants();
            var data = TypeSearcherItemData.Constant(Stencil.GenerateTypeHandle(typeof(string)));

            Assert.IsTrue(filter.ApplyFilters(data));
        }

        [TestCase(typeof(TestingMode), true)]
        [TestCase(typeof(SearcherFilter), false)]
        public void TestWithEnums(Type type, bool expectedResult)
        {
            var filter = new SearcherFilter(SearcherContext.Type).WithEnums(Stencil);
            var data = new TypeSearcherItemData(type.GenerateTypeHandle(Stencil));

            Assert.AreEqual(expectedResult, filter.ApplyFilters(data));
        }

        class TestFilter : SearcherFilter
        {
            public TestFilter(SearcherContext context)
                : base(context) {}

            internal TestFilter WithString()
            {
                this.Register((Func<TypeSearcherItemData, bool>)(d => d.Type == TypeHandle.String));
                return this;
            }

            internal TestFilter WithInt()
            {
                this.Register((Func<TypeSearcherItemData, bool>)(d => d.Type == TypeHandle.Int));
                return this;
            }
        }

        [TestCase(typeof(string), true)]
        [TestCase(typeof(int), true)]
        [TestCase(typeof(float), false)]
        public void TestMultipleFilters(Type type, bool expectedResult)
        {
            var filter = new TestFilter(SearcherContext.Type).WithInt().WithString();
            var data = new TypeSearcherItemData(type.GenerateTypeHandle(Stencil));

            Assert.AreEqual(expectedResult, filter.ApplyFilters(data));
        }
    }
}
