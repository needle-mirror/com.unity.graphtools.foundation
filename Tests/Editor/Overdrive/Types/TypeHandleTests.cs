using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Compilation;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Types
{
    namespace NewNamespace
    {
        [MovedFrom(false, sourceNamespace: "UnityEditor.GraphToolsFoundation.Overdrive.Tests.Types.OldNamespace", sourceClassName: "OldTypeName", sourceAssembly: "Unity.OldAssemblyName.Foundation.Overdrive.Editor.Tests")]
        class NewTypeName {}

        class EnclosingType
        {
            [MovedFrom(false, sourceClassName: "EnclosingType/InnerOld", sourceNamespace: "UnityEditor.GraphToolsFoundation.Overdrive.Tests.Types.OldNamespace", sourceAssembly: "Unity.OldAssemblyName.Foundation.Overdrive.Editor.Tests")]
            public class InnerNew {}

            [MovedFrom(false, sourceNamespace: "UnityEditor.GraphToolsFoundation.Overdrive.Tests.Types.OldNamespace", sourceAssembly: "Unity.OldAssemblyName.Foundation.Overdrive.Editor.Tests")]
            public class InnerTypeUnchanged {}
        }
    }

    class TypeHandleTests
    {
        CSharpTypeSerializer m_TypeSerializer;

        class TestStencil : Stencil
        {
            public override ISearcherDatabaseProvider GetSearcherDatabaseProvider()
            {
                return new ClassSearcherDatabaseProvider(this);
            }

            public override IBuilder Builder => null;
        }

        Stencil m_Stencil;

        [SetUp]
        public void SetUp()
        {
            m_Stencil = new TestStencil();
            m_TypeSerializer = new CSharpTypeSerializer();
        }

        [TearDown]
        public void TearDown()
        {
            m_Stencil = null;
        }

        [Test]
        public void Test_TypeHandleSerializationOfCustomType_Unknown()
        {
            //Arrange-Act
            var th = typeof(Unknown).GenerateTypeHandle();

            //Assert
            Assert.That(th, Is.EqualTo(TypeHandle.Unknown));
        }

        [Test]
        public void Test_TypeHandleSerializationOfCustomType_Unknown_UsingExtenderMethod()
        {
            //Arrange-Act
            var th = typeof(Unknown).GenerateTypeHandle();

            //Assert
            Assert.That(th, Is.EqualTo(TypeHandle.Unknown));
        }

        class A {}

        class B {}

        [Test]
        public void Test_TypeHandleDeserializationOfRenamedType()
        {
            var typeSerializer = new CSharpTypeSerializer(new Dictionary<string, string>
            {
                {typeof(A).AssemblyQualifiedName, typeof(B).AssemblyQualifiedName}
            });

            TypeHandle th = typeof(A).GenerateTypeHandle();

            Type deserializedTypeHandle = th.Resolve(typeSerializer);

            Assert.That(deserializedTypeHandle, Is.EqualTo(typeof(B)));
        }

        [Test]
        public void Test_TypeHandleDeserializationOfRegularType()
        {
            //Arrange
            TypeHandle th = typeof(A).GenerateTypeHandle();

            //Act
            Type deserializedTypeHandle = th.Resolve(m_TypeSerializer);

            //Assert
            Assert.That(deserializedTypeHandle, Is.EqualTo(typeof(A)));
        }

        [TestCase(typeof(int), true, false, false)]
        [TestCase(typeof(object), false, true, false)]
        [TestCase(typeof(BindingFlags), true, false, true)]
        public void Test_TypeHandleMetadataProperties(Type type, bool isValueType, bool isClass, bool isEnum)
        {
            var metadata = type.GetMetadata(m_Stencil);
            Assert.AreEqual(isValueType, metadata.IsValueType);
            Assert.AreEqual(isClass, metadata.IsClass);
            Assert.AreEqual(isEnum, metadata.IsEnum);
        }

        [Test]
        public void Test_TypeHandle_Resolve_WorksWithRenamedTypes_WithMovedFromAttribute()
        {
            var typeStr = typeof(NewNamespace.NewTypeName).AssemblyQualifiedName;
            var originalTypeStr = typeStr
                .Replace("NewNamespace", "OldNamespace")
                .Replace("NewTypeName", "OldTypeName")
                .Replace("GraphTools.", "OldAssemblyName.");

            var typeHandle = new TypeHandle(originalTypeStr);

            var resolvedType = typeHandle.Resolve(m_TypeSerializer);
            Assert.AreEqual(typeof(NewNamespace.NewTypeName), resolvedType);
        }

        [Test]
        public void Test_TypeHandle_WithNestedType_Resolve_WorksWithRenamedTypes_WithMovedFromAttribute()
        {
            var typeStr = typeof(NewNamespace.EnclosingType.InnerNew).AssemblyQualifiedName;
            var originalTypeStr = typeStr
                .Replace("NewNamespace", "OldNamespace")
                .Replace("InnerNew", "InnerOld")
                .Replace("GraphTools.", "OldAssemblyName.");

            var typeHandle = new TypeHandle(originalTypeStr);

            var resolvedType = typeHandle.Resolve(m_TypeSerializer);
            Assert.AreEqual(typeof(NewNamespace.EnclosingType.InnerNew), resolvedType);
        }

        [Test]
        public void Test_TypeHandle_WithNestedType_Resolve_ChangedAssembly_WithMovedFromAttribute()
        {
            var typeStr = typeof(NewNamespace.EnclosingType.InnerTypeUnchanged).AssemblyQualifiedName;
            var originalTypeStr = typeStr
                .Replace("NewNamespace", "OldNamespace")
                .Replace("GraphTools.", "OldAssemblyName.");

            var typeHandle = new TypeHandle(originalTypeStr);

            var resolvedType = typeHandle.Resolve(m_TypeSerializer);
            Assert.AreEqual(typeof(NewNamespace.EnclosingType.InnerTypeUnchanged), resolvedType);
        }
    }
}
