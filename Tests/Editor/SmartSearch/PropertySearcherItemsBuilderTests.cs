using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Moq;
using NUnit.Framework;
using UnityEditor.Searcher;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.SmartSearch
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    class PropertySearcherItemsBuilderTests
    {
#pragma warning disable CS0414
        class RootClass
        {
            public ChildClass publicMember = null;
            protected ChildClass m_ProtectedMember = null;
            ChildClass m_PrivateMember;
        }

        public class ChildClass
        {
            public ChildClass publicChild = null;
            protected ChildClass m_ProtectedChild = null;
            ChildClass m_PrivateChild = null;
        }
#pragma warning restore CS0414

        public class RecursiveClass
        {
            public RecursiveClass recursiveChild = null;
        }

        static readonly TypeHandle k_RootHandle = new TypeHandle("__ROOT_CLASS");
        static readonly TypeHandle k_ChildHandle = new TypeHandle("__CHILD_CLASS");
        static readonly TypeHandle k_RecursiveHandle = new TypeHandle("__RECURSIVE_CLASS");

        ITypeMetadata m_RootMetadata;
        ITypeMetadata m_ChildMetadata;
        ITypeMetadata m_RecursiveMetadata;

        readonly List<MemberInfoValue> m_RootPublicMembers = new List<MemberInfoValue>
        {
            new MemberInfoValue(k_RootHandle, k_ChildHandle, nameof(RootClass.publicMember), MemberTypes.Field)
        };
        readonly List<MemberInfoValue> m_RootNonPublicMembers = new List<MemberInfoValue>
        {
            new MemberInfoValue(k_RootHandle, k_ChildHandle, "m_ProtectedMember", MemberTypes.Field),
            new MemberInfoValue(k_RootHandle, k_ChildHandle, "m_PrivateMember", MemberTypes.Field)
        };

        readonly List<MemberInfoValue> m_ChildPublicMembers = new List<MemberInfoValue>
        {
            new MemberInfoValue(k_ChildHandle, k_ChildHandle, nameof(ChildClass.publicChild), MemberTypes.Field)
        };
        readonly List<MemberInfoValue> m_ChildNonPublicMembers = new List<MemberInfoValue>
        {
            new MemberInfoValue(k_ChildHandle, k_ChildHandle, "m_ProtectedChild", MemberTypes.Field),
            new MemberInfoValue(k_ChildHandle, k_ChildHandle, "m_PrivateChild", MemberTypes.Field)
        };

        readonly List<MemberInfoValue> m_RecursivePublicMembers = new List<MemberInfoValue>
        {
            new MemberInfoValue(k_RecursiveHandle, k_RecursiveHandle, nameof(RecursiveClass.recursiveChild), MemberTypes.Field)
        };
        readonly List<MemberInfoValue> m_RecursiveNonPublicMembers = new List<MemberInfoValue>();

        [SetUp]
        public void SetUp()
        {
            var rootMetadataMock = new Mock<ITypeMetadata>();
            rootMetadataMock.SetupGet(meta => meta.TypeHandle).Returns(k_RootHandle);
            rootMetadataMock.SetupGet(meta => meta.PublicMembers).Returns(m_RootPublicMembers);
            rootMetadataMock.SetupGet(meta => meta.NonPublicMembers).Returns(m_RootNonPublicMembers);
            m_RootMetadata = rootMetadataMock.Object;

            var childMetadataMock = new Mock<ITypeMetadata>();
            childMetadataMock.SetupGet(meta => meta.TypeHandle).Returns(k_ChildHandle);
            childMetadataMock.SetupGet(meta => meta.PublicMembers).Returns(m_ChildPublicMembers);
            childMetadataMock.SetupGet(meta => meta.NonPublicMembers).Returns(m_ChildNonPublicMembers);
            m_ChildMetadata = childMetadataMock.Object;

            var recursiveMetadataMock = new Mock<ITypeMetadata>();
            recursiveMetadataMock.SetupGet(meta => meta.TypeHandle).Returns(k_RecursiveHandle);
            recursiveMetadataMock.SetupGet(meta => meta.PublicMembers).Returns(m_RecursivePublicMembers);
            recursiveMetadataMock.SetupGet(meta => meta.NonPublicMembers).Returns(m_RecursiveNonPublicMembers);
            m_RecursiveMetadata = recursiveMetadataMock.Object;
        }

        ITypeMetadataResolver GetMockedTypeMetadataResolver()
        {
            var mock = new Mock<ITypeMetadataResolver>();
            mock.Setup(resolver => resolver.Resolve(It.Is<TypeHandle>(th => th == k_RootHandle))).Returns(m_RootMetadata);
            mock.Setup(resolver => resolver.Resolve(It.Is<TypeHandle>(th => th == k_ChildHandle))).Returns(m_ChildMetadata);
            mock.Setup(resolver => resolver.Resolve(It.Is<TypeHandle>(th => th == k_RecursiveHandle))).Returns(m_RecursiveMetadata);
            return mock.Object;
        }

        static PropertySearcherItemsBuilder CreateSearcherItem(int recursion, TypeHandle type, ITypeMetadataResolver resolver)
        {
            return new PropertySearcherItemsBuilder(recursion, type, resolver, new HashSet<int>());
        }

        [Test]
        public void Should_ReturnNonPublicMembers_When_BuildingRootTypeMembers()
        {
            //Arrange
            ITypeMetadataResolver resolver = GetMockedTypeMetadataResolver();
            var builder = CreateSearcherItem(0, k_RootHandle, resolver);

            //Act
            List<SearcherItem> searcherItems = builder.Build();

            //Assert
            //That all Non public members are returned
            foreach (var member in m_RootMetadata.NonPublicMembers)
            {
                Assert.That(searcherItems.Select(i => i.Name), Has.Member(member.Name));
            }
        }

        [Test]
        public void Should_ReturnPublicMembers_When_BuildingRootTypeMembers()
        {
            //Arrange
            ITypeMetadataResolver resolver = GetMockedTypeMetadataResolver();
            var builder = CreateSearcherItem(0, k_RootHandle, resolver);

            //Act
            List<SearcherItem> searcherItems = builder.Build();

            //Assert
            //That all public members are returned
            foreach (var member in m_RootMetadata.PublicMembers)
            {
                Assert.That(searcherItems.Select(i => i.Name), Has.Member(member.Name));
            }
        }

        [Test]
        public void Should_ReturnPublicMembers_When_BuildingChildTypeMembers()
        {
            //Arrange
            ITypeMetadataResolver resolver = GetMockedTypeMetadataResolver();
            var builder = CreateSearcherItem(1, k_RootHandle, resolver);

            //Act
            List<SearcherItem> searcherItems = builder.Build();

            //Assert
            //That all child public members are returned
            foreach (var member in m_ChildMetadata.PublicMembers)
            {
                Assert.That(searcherItems[0].Children.Select(i => i.Name), Has.Member(member.Name));
            }
        }

        [Test]
        public void ShouldNot_ReturnNonPublicMembers_When_BuildingChildTypeMembers()
        {
            //Arrange
            ITypeMetadataResolver resolver = GetMockedTypeMetadataResolver();
            var builder = CreateSearcherItem(1, k_RootHandle, resolver);

            //Act
            List<SearcherItem> searcherItems = builder.Build();

            //Assert
            //That no child public members are returned
            foreach (var member in m_ChildMetadata.NonPublicMembers)
            {
                Assert.That(searcherItems[0].Children.Select(i => i.Name), Has.No.Member(member.Name));
            }
        }

        [Test]
        public void Test_Recursivity_ReturnsProperAmount()
        {
            //Arrange
            int recursiveDepth = 7;
            ITypeMetadataResolver resolver = GetMockedTypeMetadataResolver();
            var builder = CreateSearcherItem(recursiveDepth, k_RecursiveHandle, resolver);

            //Act
            List<SearcherItem> searcherItems = builder.Build();
            List<SearcherItem> childrenItems = searcherItems[0].Children;
            int depthCount = 0;
            while (childrenItems.Count > 0)
            {
                depthCount++;
                childrenItems = childrenItems[0].Children;
            }

            //Assert
            Assert.That(depthCount, Is.EqualTo(recursiveDepth));
        }

        [Test]
        public void Test_Recursivity_CreateSearcherItemsWithProperParent()
        {
            //Arrange
            string rootPublicName = nameof(RootClass.publicMember);
            ITypeMetadataResolver resolver = GetMockedTypeMetadataResolver();
            var builder = CreateSearcherItem(1, k_RootHandle, resolver);

            //Act
            List<SearcherItem> searcherItems = builder.Build();

            //Assert
            var rootPublicMemberSearcherItem = searcherItems.First(item => item.Name == rootPublicName);
            var childSearcherItem = rootPublicMemberSearcherItem.Children[0];
            Assert.That(childSearcherItem.Parent, Is.EqualTo(rootPublicMemberSearcherItem));
        }
    }
}
