using System;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEditor.VisualScripting.Model.Compilation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScriptingTests.SmartSearch
{
    class TypeSearcherItemTests
    {
        sealed class TestStencil : Stencil
        {
            public override ISearcherDatabaseProvider GetSearcherDatabaseProvider()
            {
                return new ClassSearcherDatabaseProvider(this);
            }

            [CanBeNull]
            public override IBuilder Builder => null;
        }

        [TestCase]
        public void TestTypeSearcherItemSystemObject()
        {
            Stencil stencil = new TestStencil();
            var item = new TypeSearcherItem(typeof(object).GenerateTypeHandle(stencil), "System.Object");

            Assert.AreEqual(item.Type, typeof(object).GenerateTypeHandle(stencil));
            Assert.AreEqual(item.Name, "System.Object");
        }

        [TestCase]
        public void TestTypeSearcherItemUnityObject()
        {
            Stencil stencil = new TestStencil();
            var item = new TypeSearcherItem(typeof(Object).GenerateTypeHandle(stencil), "UnityEngine.Object");

            Assert.AreEqual(item.Type, typeof(Object).GenerateTypeHandle(stencil));
            Assert.AreEqual(item.Name, "UnityEngine.Object");
        }

        [TestCase]
        public void TestTypeSearcherItemString()
        {
            Stencil stencil = new TestStencil();
            var item = new TypeSearcherItem(typeof(string).GenerateTypeHandle(stencil), typeof(string).FriendlyName());

            Assert.AreEqual(item.Type, typeof(string).GenerateTypeHandle(stencil));
            Assert.AreEqual(item.Name, typeof(string).FriendlyName());
        }
    }
}
