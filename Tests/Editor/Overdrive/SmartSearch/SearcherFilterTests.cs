using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Moq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch;
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
                yield return new TestCaseData(new Mock<IGTFGraphAssetModel>().Object, true);
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
            var data = new TypeSearcherItemData(type.GenerateTypeHandle());

            Assert.AreEqual(expectedResult, filter.ApplyFilters(data));
        }
    }
}
