using System;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEditor.EditorCommon.Extensions;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEditor.VisualScripting.Model.Compilation;
using UnityEngine;
using UnityEngine.TestTools;

public class DummyTestType { }

namespace UnityEditor.VisualScriptingTests.Models
{
    class StencilTests
    {
        class TestStencil : Stencil
        {
            public override ISearcherDatabaseProvider GetSearcherDatabaseProvider()
            {
                return new ClassSearcherDatabaseProvider(this);
            }

            public override IBuilder Builder => null;
        }

        [Test]
        public void TestCanLoadAllTypesFromAssemblies()
        {
            // If this test fails, failing assemblies must be added to the Stencil.BlackListedAssemblies
            Assert.DoesNotThrow(() =>
            {
                var stencil = new TestStencil();
                var types = stencil.GetAssemblies()
                    .SelectMany(a => a.GetTypesSafe(), (domainAssembly, assemblyType) => assemblyType)
                    .Where(t => !t.IsAbstract && !t.IsInterface);
                Assert.IsNotNull(types);
            });

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void TestParallelGetAssembliesTypes_ClassStencil()
        {
            var methodInfo = typeof(ClassStencil).GetMethod("IsValidType", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(methodInfo);

            var stencil = new ClassStencil();
            var result = stencil.GetAssembliesTypesMetadata();
            var expectedResult = stencil.GetAssemblies()
                .SelectMany(a => a.GetTypesSafe())
                .Where(x => Convert.ToBoolean(methodInfo.Invoke(null, new object[] { x })))
                .Select(t => stencil.GenerateTypeHandle(t).GetMetadata(stencil))
                .ToList();
            expectedResult.Sort((x, y) => string.CompareOrdinal(x.TypeHandle.Identification, y.TypeHandle.Identification));

            Assert.That(result, Is.TypeHandleCollectionEquivalent(expectedResult));
        }

        [TestCase(typeof(string), ExpectedResult = true, TestName = "TestValidType")]
        [TestCase(typeof(IDisposable), ExpectedResult = false, TestName = "TestInterface")]
        [TestCase(typeof(Stencil), ExpectedResult = false, TestName = "TestAbstractType")]
        [TestCase(typeof(Transform), ExpectedResult = true, TestName = "TestUnityEngineComponent")]
        [TestCase(typeof(StencilTests), ExpectedResult = false, TestName = "TestPrivateType")]
        [TestCase(typeof(DummyTestType), ExpectedResult = true, TestName = "TestPublicTypeWithNoNamespace")]
        [TestCase(typeof(PublicAPIAttribute), ExpectedResult = false, TestName = "TestTypeWithBlackListedNamespace")]
        public bool TestIsValidType(Type type)
        {
            var methodInfo = typeof(ClassStencil).GetMethod("IsValidType", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(methodInfo);

            return Convert.ToBoolean(methodInfo.Invoke(null, new object[] { type }));
        }
    }
}
