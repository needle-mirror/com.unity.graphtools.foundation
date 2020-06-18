using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;
using UnityEngine;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Models
{
    class GenericsInferenceSolverTests : BaseFixture
    {
        internal enum ClassType
        {
            Plain,
            Generic,
        }

        [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
        class D<T>
        {
            public void F(T t) {}
            public T G(T t)
            {
                return t;
            }

            public T H(T t, T t2)
            {
                return t;
            }

            public List<T> ListFromType(T t)
            {
                return null;
            }

            public T TypeFromList(List<T> t)
            {
                return default(T);
            }

            public List<T> ListFromList(List<T> t)
            {
                return t;
            }
        }

        [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
        class C
        {
            public void F<T>(T t) {}

            public T G<T>(T t)
            {
                return t;
            }

            public T H<T>(T t, T t2)
            {
                return t;
            }

            public T I<T>(T t, int i)
            {
                return t;
            }

            public List<T> ListFromType<T>(T t)
            {
                return null;
            }

            public T TypeFromList<T>(List<T> t)
            {
                return default(T);
            }

            public List<T> ListFromList<T>(List<T> t)
            {
                return t;
            }
        }

        [TestCase(ClassType.Plain, "F",            typeof(float),       0)]
        [TestCase(ClassType.Plain, "G",            typeof(float),       0)]
        [TestCase(ClassType.Plain, "H",            typeof(float),       0)]
        [TestCase(ClassType.Plain, "H",            typeof(float),       1)]
        [TestCase(ClassType.Plain, "ListFromType", typeof(float),       0)]
        [TestCase(ClassType.Plain, "TypeFromList", typeof(List<float>), 0)]
        [TestCase(ClassType.Plain, "ListFromList", typeof(List<float>), 0)]

        [TestCase(ClassType.Generic, "F",            typeof(float),       0)]
        [TestCase(ClassType.Generic, "G",            typeof(float),       0)]
        [TestCase(ClassType.Generic, "H",            typeof(float),       0)]
        [TestCase(ClassType.Generic, "H",            typeof(float),       1)]
        [TestCase(ClassType.Generic, "ListFromType", typeof(float),       0)]
        [TestCase(ClassType.Generic, "TypeFromList", typeof(List<float>), 0)]
        [TestCase(ClassType.Generic, "ListFromList", typeof(List<float>), 0)]
        public void SolveTypeArguments(ClassType t, string methodName, Type connectedType, int parameterIndex)
        {
            Type methodDeclaringType = t == ClassType.Plain ? typeof(C) : typeof(D<>);
            MethodBase m = methodDeclaringType.GetMethod(methodName);
            TypeHandle[] typeReferences = new TypeHandle[1];
            Assume.That(m, Is.Not.Null);
            Type[] genericTypes = null;
            GenericsInferenceSolver.SolveTypeArguments(Stencil, m, ref genericTypes, ref typeReferences, connectedType, parameterIndex);
            Assert.That(genericTypes.Length, Is.EqualTo(1));
            Assert.That(typeReferences[0].Resolve(Stencil), Is.EqualTo(typeof(float)));
        }

        [Test]
        public void ConnectingANonGenericParameterDoesntAffectTypeParameters()
        {
            Type methodDeclaringType = typeof(C);
            MethodBase m = methodDeclaringType.GetMethod("I");
            TypeHandle[] typeReferences = new TypeHandle[1];
            Assume.That(m, Is.Not.Null);
            Type[] genericTypes = null;
            GenericsInferenceSolver.SolveTypeArguments(Stencil, m, ref genericTypes, ref typeReferences, typeof(float), 0);
            GenericsInferenceSolver.SolveTypeArguments(Stencil, m, ref genericTypes, ref typeReferences, typeof(int), 1);
            Assert.That(genericTypes.Length, Is.EqualTo(1));
            Assert.That(typeReferences[0].Resolve(Stencil), Is.EqualTo(typeof(float)));
        }

        protected override bool CreateGraphOnStartup => true;
    }
}
