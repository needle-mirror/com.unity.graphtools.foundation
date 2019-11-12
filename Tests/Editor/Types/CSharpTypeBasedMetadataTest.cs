using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Moq;
using NUnit.Framework;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Types
{
    class CSharpTypeBasedMetadataTest
    {
        readonly TypeHandle m_Handle = new TypeHandle("__TYPEHANDLE");
        readonly CSharpTypeSerializer m_Serializer = new CSharpTypeSerializer();
        TypeHandle m_TestMemberClassTypeHandle;
        TypeHandle m_IntHandle;
        TypeHandle m_FloatHandle;

        [SetUp]
        public void SetUp()
        {
            m_IntHandle = m_Serializer.GenerateTypeHandle(typeof(int));
            m_FloatHandle = m_Serializer.GenerateTypeHandle(typeof(float));
            m_TestMemberClassTypeHandle = m_Serializer.GenerateTypeHandle(typeof(TestMemberClass));
        }

        class TestGraphContext : GraphContext
        {
            public override bool MemberAllowed(MemberInfoValue value)
            {
                return value.Name != nameof(TestMemberClass.BlacklistedField) &&
                    value.Name != nameof(TestMemberClass.BlacklistedProperty);
            }
        }

        CSharpTypeBasedMetadata CreateMetadata(Type t)
        {
            return new CSharpTypeBasedMetadata(new TestGraphContext(), m_Handle, t);
        }

        [Test]
        public void Test_TypeHandle()
        {
            //Arrange
            var typeMetadata = CreateMetadata(typeof(float));

            //Act
            TypeHandle handle = typeMetadata.TypeHandle;

            //Assert
            Assert.That(handle, Is.EqualTo(m_Handle));
        }

        [Test]
        public void Test_Name()
        {
            //Arrange
            var floatType = typeof(float);
            var typeMetadata = CreateMetadata(floatType);

            //Act
            string typeName = typeMetadata.Name;

            //Assert
            Assert.That(typeName, Is.EqualTo(floatType.Name));
        }

        [Test]
        public void Test_FriendlyName()
        {
            //Arrange
            var typeMetadata = CreateMetadata(typeof(float));

            //Act
            string friendlyName = typeMetadata.FriendlyName;

            //Assert
            Assert.That(friendlyName, Is.EqualTo("Float"));
        }

        [Test]
        public void Test_GenericArguments()
        {
            //Arrange
            var dictionaryType = typeof(Dictionary<int, float>);
            var typeMetadata = CreateMetadata(dictionaryType);

            //Act
            List<TypeHandle> genericArguments = typeMetadata.GenericArguments.ToList();

            //Assert
            var expectedResult = new List<TypeHandle> { m_IntHandle, m_FloatHandle };
            CollectionAssert.AreEqual(genericArguments, expectedResult);
        }

        [Test]
        public void Test_IsAssignableFrom_UsingMetadata_DelegatesToOtherMetadata()
        {
            //Arrange
            var typeMetadata = CreateMetadata(typeof(Dictionary<int, float>));
            var mockMetadata = new Mock<ITypeMetadata>();
            mockMetadata.Setup(meta => meta.IsAssignableTo(It.IsAny<Type>())).Returns(true);

            //Act
            bool isAssignableFrom = typeMetadata.IsAssignableFrom(mockMetadata.Object);

            //Assert
            mockMetadata.Verify(metadata => metadata.IsAssignableTo(It.IsAny<Type>()), Times.Once());
            Assert.That(isAssignableFrom, Is.True);
        }

        [Test]
        public void Test_IsAssignableFrom_UsingType()
        {
            //Arrange
            var typeMetadata = CreateMetadata(typeof(IDictionary<int, float>));

            //Act
            bool isAssignableFrom = typeMetadata.IsAssignableFrom(typeof(Dictionary<int, float>));

            //Assert
            Assert.That(isAssignableFrom, Is.True);
        }

        [Test]
        public void Test_IsAssignableTo_UsingMetadata_DelegatesToOtherMetadata()
        {
            //Arrange
            var typeMetadata = CreateMetadata(typeof(Dictionary<int, float>));
            var mockMetadata = new Mock<ITypeMetadata>();
            mockMetadata.Setup(meta => meta.IsAssignableFrom(It.IsAny<Type>())).Returns(true);

            //Act
            bool isAssignableTo = typeMetadata.IsAssignableTo(mockMetadata.Object);

            //Assert
            mockMetadata.Verify(metadata => metadata.IsAssignableFrom(It.IsAny<Type>()), Times.Once());
            Assert.That(isAssignableTo, Is.True);
        }

        [Test]
        public void Test_IsAssignableTo_UsingType()
        {
            //Arrange
            var typeMetadata = CreateMetadata(typeof(Dictionary<int, float>));

            //Act
            bool isAssignableTo = typeMetadata.IsAssignableTo(typeof(IDictionary<int, float>));

            //Assert
            Assert.That(isAssignableTo, Is.True);
        }

        [Test, Ignore("mb")]
        public void Test_IsAssignableTo_UsingGraph()
        {
            throw new NotImplementedException("Test_IsAssignableTo_UsingGraph");
//            //Arrange
//            var typeMetadata = CreateMetadata(typeof(VisualBehaviour));
//
//            //Act
//            bool isAssignableTo = typeMetadata.IsAssignableTo(new Mock<IVSGraphModel>().Object);
//
//            //Assert
//            Assert.That(isAssignableTo, Is.False);
        }

        [Test]
        public void Test_IsSubclassOf_UsingMetadata_DelegatesToOtherMetadata()
        {
            //Arrange
            var dictionaryType = typeof(Dictionary<int, float>);
            var typeMetadata = CreateMetadata(dictionaryType);
            var mockMetadata = new Mock<ITypeMetadata>();
            mockMetadata.Setup(meta => meta.IsSuperclassOf(It.IsAny<Type>())).Returns(true);

            //Act
            bool isSubclassOf = typeMetadata.IsSubclassOf(mockMetadata.Object);

            //Assert
            mockMetadata.Verify(metadata => metadata.IsSuperclassOf(It.IsAny<Type>()), Times.Once());
            Assert.That(isSubclassOf, Is.True);
        }

        [Test]
        public void Test_IsSuperclassOf_UsingMetadata_DelegatesToOtherMetadata()
        {
            //Arrange
            var dictionaryType = typeof(Dictionary<int, float>);
            var typeMetadata = CreateMetadata(dictionaryType);
            var mockMetadata = new Mock<ITypeMetadata>();
            mockMetadata.Setup(meta => meta.IsSubclassOf(It.IsAny<Type>())).Returns(true);

            //Act
            bool isSuperclassOf = typeMetadata.IsSuperclassOf(mockMetadata.Object);

            //Assert
            mockMetadata.Verify(metadata => metadata.IsSubclassOf(It.IsAny<Type>()), Times.Once());
            Assert.That(isSuperclassOf, Is.True);
        }

        [Test]
        public void Test_GetMembers_OnEnum_ReturnsNothing()
        {
            //Arrange
            var enumType = typeof(MemberTypes);
            var typeMetadata = CreateMetadata(enumType);

            //Act
            List<MemberInfoValue> members = typeMetadata.PublicMembers.Concat(typeMetadata.NonPublicMembers).ToList();

            //Assert
            CollectionAssert.IsEmpty(members);
        }

        [Test]
        public void Test_GetPublicMembers_ReturnsPublicFieldsAndProperties()
        {
            //Arrange
            var testType = typeof(TestMemberClass);
            var typeMetadata = CreateMetadata(testType);

            //Act
            List<MemberInfoValue> members = typeMetadata.PublicMembers;

            //Assert
            List<MemberInfoValue> expectedEntries = new List<MemberInfoValue>
            {
                new MemberInfoValue(m_TestMemberClassTypeHandle, m_IntHandle, "PublicField", MemberTypes.Field),
                new MemberInfoValue(m_TestMemberClassTypeHandle, m_IntHandle, "PublicProperty", MemberTypes.Property),
            };
            CollectionAssert.AreEquivalent(expectedEntries, members);
        }

        [Test]
        public void Test_GetNonPublicMembers_ReturnsProtectedOrPrivate_FieldsAndProperties()
        {
            //Arrange
            var testType = typeof(TestMemberClass);
            var typeMetadata = CreateMetadata(testType);

            //Act
            List<MemberInfoValue> members = typeMetadata.NonPublicMembers;

            //Assert
            List<MemberInfoValue> expectedEntries = new List<MemberInfoValue>
            {
                new MemberInfoValue(m_TestMemberClassTypeHandle, m_IntHandle, "m_ProtectedField", MemberTypes.Field),
                new MemberInfoValue(m_TestMemberClassTypeHandle, m_IntHandle, "m_PrivateField", MemberTypes.Field),

                new MemberInfoValue(m_TestMemberClassTypeHandle, m_IntHandle, "ProtectedProperty", MemberTypes.Property),
                new MemberInfoValue(m_TestMemberClassTypeHandle, m_IntHandle, "PrivateProperty", MemberTypes.Property),
            };
            CollectionAssert.AreEquivalent(members, expectedEntries);
        }

        [Test]
        public void Test_GetMembers_Filters_ObsoleteMembers()
        {
            //Arrange
            var testType = typeof(TestMemberClass);
            var typeMetadata = CreateMetadata(testType);

            //Act
            List<MemberInfoValue> members = typeMetadata.PublicMembers.Concat(typeMetadata.NonPublicMembers).ToList();

            //Assert
            List<MemberInfoValue> expectedMissingEntries = new List<MemberInfoValue>
            {
                new MemberInfoValue(m_TestMemberClassTypeHandle, m_IntHandle, "ObsoleteField", MemberTypes.Field),
                new MemberInfoValue(m_TestMemberClassTypeHandle, m_IntHandle, "ObsoleteProperty", MemberTypes.Property),
            };

            var remainingObsoleteMembers = members.Intersect(expectedMissingEntries);
            Assert.That(remainingObsoleteMembers, Is.Empty);
        }

        [Test]
        public void Test_GetMembers_Filters_BlacklistedMembers()
        {
            //Arrange
            var testType = typeof(TestMemberClass);
            var typeMetadata = CreateMetadata(testType);

            //Act
            List<MemberInfoValue> members = typeMetadata.PublicMembers.Concat(typeMetadata.NonPublicMembers).ToList();

            //Assert
            List<MemberInfoValue> expectedMissingEntries = new List<MemberInfoValue>
            {
                new MemberInfoValue(m_TestMemberClassTypeHandle, m_IntHandle, "BlacklistedField", MemberTypes.Field),
                new MemberInfoValue(m_TestMemberClassTypeHandle, m_IntHandle, "BlacklistedProperty", MemberTypes.Property),
            };

            var remainingObsoleteMembers = members.Intersect(expectedMissingEntries);
            Assert.That(remainingObsoleteMembers, Is.Empty);
        }

        [Test]
        public void Test_GetMembers_Filters_CompilerGeneratedFields()
        {
            //Arrange
            var testType = typeof(TestMemberClass);
            var typeMetadata = CreateMetadata(testType);

            //Act
            List<MemberInfoValue> members = typeMetadata.PublicMembers.Concat(typeMetadata.NonPublicMembers).ToList();

            //Assert
            List<MemberInfoValue> expectedMissingEntries = new List<MemberInfoValue>
            {
                new MemberInfoValue(m_TestMemberClassTypeHandle, m_IntHandle, "CompilerGeneratedField", MemberTypes.Field),
                new MemberInfoValue(m_TestMemberClassTypeHandle, m_IntHandle, "CompilerGeneratedProperty", MemberTypes.Property),
            };

            var remainingObsoleteMembers = members.Intersect(expectedMissingEntries);
            Assert.That(remainingObsoleteMembers, Is.Empty);
        }

        [Test]
        public void Test_GetMembers_Filters_PropertiesWithParams()
        {
            //Arrange
            var enumType = typeof(TestMemberClass);
            var typeMetadata = CreateMetadata(enumType);

            //Act
            List<MemberInfoValue> members = typeMetadata.PublicMembers.Concat(typeMetadata.NonPublicMembers).ToList();

            //Assert
            Assert.That(members, Has.No.Contains(new MemberInfoValue(m_TestMemberClassTypeHandle, m_IntHandle, "Item", MemberTypes.Property)));
        }

#pragma warning disable CS0414
        [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
        class TestMemberClass
        {
            public int this[int index] => index;

            public int PublicField = 1;
            public int PublicProperty => 2;

            protected int m_ProtectedField = 3;
            protected int ProtectedProperty => 4;

            int m_PrivateField = 5;
            int PrivateProperty => 6;

            public int BlacklistedField = 7;
            public int BlacklistedProperty => 8;

            [Obsolete]
            public int ObsoleteField = 9;
            [Obsolete]
            public int ObsoleteProperty => 10;

            [CompilerGenerated]
            public int CompilerGeneratedField = 11;
            [CompilerGenerated]
            public int CompilerGeneratedProperty => 12;
        }
#pragma warning restore CS0414
    }
}
