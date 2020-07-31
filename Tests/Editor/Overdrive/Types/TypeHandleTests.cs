using System;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.Types.NewNamespace;
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

        [Test]
        public void Test_TypeHandleDeserializationOfRegularType()
        {
            //Arrange
            TypeHandle th = typeof(A).GenerateTypeHandle();

            //Act
            Type deserializedTypeHandle = th.Resolve();

            //Assert
            Assert.That(deserializedTypeHandle, Is.EqualTo(typeof(A)));
        }

        [Test]
        public void Test_TypeHandle_Resolve_WorksWithRenamedTypes_WithMovedFromAttribute()
        {
            var typeStr = typeof(NewTypeName).AssemblyQualifiedName;
            var originalTypeStr = typeStr ?
                .Replace("NewNamespace", "OldNamespace")
                .Replace("NewTypeName", "OldTypeName")
                .Replace("GraphTools.", "OldAssemblyName.");

            var typeHandle = new TypeHandle(originalTypeStr);

            var resolvedType = typeHandle.Resolve();
            Assert.AreEqual(typeof(NewTypeName), resolvedType);
        }

        [Test]
        public void Test_TypeHandle_WithNestedType_Resolve_WorksWithRenamedTypes_WithMovedFromAttribute()
        {
            var typeStr = typeof(EnclosingType.InnerNew).AssemblyQualifiedName;
            var originalTypeStr = typeStr ?
                .Replace("NewNamespace", "OldNamespace")
                .Replace("InnerNew", "InnerOld")
                .Replace("GraphTools.", "OldAssemblyName.");

            var typeHandle = new TypeHandle(originalTypeStr);

            var resolvedType = typeHandle.Resolve();
            Assert.AreEqual(typeof(EnclosingType.InnerNew), resolvedType);
        }

        [Test]
        public void Test_TypeHandle_WithNestedType_Resolve_ChangedAssembly_WithMovedFromAttribute()
        {
            var typeStr = typeof(EnclosingType.InnerTypeUnchanged).AssemblyQualifiedName;
            var originalTypeStr = typeStr ?
                .Replace("NewNamespace", "OldNamespace")
                .Replace("GraphTools.", "OldAssemblyName.");

            var typeHandle = new TypeHandle(originalTypeStr);

            var resolvedType = typeHandle.Resolve();
            Assert.AreEqual(typeof(EnclosingType.InnerTypeUnchanged), resolvedType);
        }
    }
}
