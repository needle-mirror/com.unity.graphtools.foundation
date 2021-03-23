using System;
using System.Diagnostics.CodeAnalysis;
using Moq;
using NUnit.Framework;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Types
{
    class TypeMetadataResolverTest
    {
        static readonly TypeHandle k_IntHandle = new TypeHandle("__INT");
        static readonly TypeHandle k_FloatHandle = new TypeHandle("__FLOAT");
        static readonly TypeHandle k_DoubleHandle = new TypeHandle("__DOUBLE");

        [Test]
        public void Should_CreateNewMetadata_OnEveryDifferentTypeHandle()
        {
            //Arrange
            var graphContext = new GraphContext();
            var resolver = new TypeMetadataResolver(graphContext);

            //Act
            var intMetadata = resolver.Resolve(k_IntHandle);
            var intMetadata2 = resolver.Resolve(k_IntHandle);
            var floatMetadata = resolver.Resolve(k_FloatHandle);
            var floatMetadata2 = resolver.Resolve(k_FloatHandle);
            var doubleMetadata = resolver.Resolve(k_DoubleHandle);
            var doubleMetadata2 = resolver.Resolve(k_DoubleHandle);

            //Assert
            Assert.That(intMetadata, Is.SameAs(intMetadata2));
            Assert.That(floatMetadata, Is.SameAs(floatMetadata2));
            Assert.That(doubleMetadata, Is.SameAs(doubleMetadata2));
        }
    }
}
