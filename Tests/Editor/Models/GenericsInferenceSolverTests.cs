using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Mode;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Models
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
            public void F(T t) { }
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
            public void F<T>(T t) { }

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

        [TestCase(ClassType.Plain, "F", typeof(float), 0)]
        [TestCase(ClassType.Plain, "G", typeof(float), 0)]
        [TestCase(ClassType.Plain, "H", typeof(float), 0)]
        [TestCase(ClassType.Plain, "H", typeof(float), 1)]
        [TestCase(ClassType.Plain, "ListFromType", typeof(float), 0)]
        [TestCase(ClassType.Plain, "TypeFromList", typeof(List<float>), 0)]
        [TestCase(ClassType.Plain, "ListFromList", typeof(List<float>), 0)]

        [TestCase(ClassType.Generic, "F", typeof(float), 0)]
        [TestCase(ClassType.Generic, "G", typeof(float), 0)]
        [TestCase(ClassType.Generic, "H", typeof(float), 0)]
        [TestCase(ClassType.Generic, "H", typeof(float), 1)]
        [TestCase(ClassType.Generic, "ListFromType", typeof(float), 0)]
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

        const int k_InstanceIndex = -1;

        [TestCase(ClassType.Plain, "F", typeof(float), 0, typeof(void))]
        [TestCase(ClassType.Plain, "G", typeof(float), 0, typeof(float))]
        [TestCase(ClassType.Plain, "H", typeof(float), 0, typeof(float))]
        [TestCase(ClassType.Plain, "H", typeof(float), 1, typeof(float))]
        [TestCase(ClassType.Plain, "ListFromType", typeof(float), 0, typeof(List<float>))]
        [TestCase(ClassType.Plain, "TypeFromList", typeof(List<float>), 0, typeof(float))]
        [TestCase(ClassType.Plain, "ListFromList", typeof(List<float>), 0, typeof(List<float>))]

        [TestCase(ClassType.Generic, "F", typeof(List<float>), k_InstanceIndex, typeof(void))]
        [TestCase(ClassType.Generic, "G", typeof(List<float>), k_InstanceIndex, typeof(float))]
        [TestCase(ClassType.Generic, "H", typeof(List<float>), k_InstanceIndex, typeof(float))]
        [TestCase(ClassType.Generic, "ListFromType", typeof(List<float>), k_InstanceIndex, typeof(List<float>))]
        [TestCase(ClassType.Generic, "TypeFromList", typeof(List<float>), k_InstanceIndex, typeof(float))]
        [TestCase(ClassType.Generic, "ListFromList", typeof(List<float>), k_InstanceIndex, typeof(List<float>))]

        [TestCase(ClassType.Generic, "F", typeof(float), 0, typeof(void))]
        [TestCase(ClassType.Generic, "G", typeof(float), 0, typeof(float))]
        [TestCase(ClassType.Generic, "H", typeof(float), 0, typeof(float))]
        [TestCase(ClassType.Generic, "H", typeof(float), 1, typeof(float))]
        [TestCase(ClassType.Generic, "ListFromType", typeof(float), 0, typeof(List<float>))]
        [TestCase(ClassType.Generic, "TypeFromList", typeof(List<float>), 0, typeof(float))]
        [TestCase(ClassType.Generic, "ListFromList", typeof(List<float>), 0, typeof(List<float>))]
        public void GenericOutputTypeIsSolved(ClassType t, string methodName, Type connectedType, int methodParameterIndex,
            Type expectedOutputType)
        {
            Type methodDeclaringType = t == ClassType.Plain ? typeof(C) : typeof(D<>);
            FunctionCallNodeModel call = GraphModel.CreateNode<FunctionCallNodeModel>("funcNode", Vector2.zero);
            call.AssetModel = GraphModel.AssetModel;
            call.MethodInfo = methodDeclaringType.GetMethod(methodName);
            Assert.That(call.MethodInfo, Is.Not.Null);
            call.DefineNode();

            IPortModel parameterPort;
            if (methodParameterIndex == k_InstanceIndex)
            {
                Assert.That(call.MethodInfo.IsStatic || call.MethodInfo.IsConstructor, Is.False);
                parameterPort = call.InstancePort;
            }
            else
            {
                var param = call.MethodInfo.GetParameters()[methodParameterIndex];
                parameterPort = call.GetPortForParameter(param.Name);
            }
            var p = new PortModel
            {
                Direction = Direction.Output,
                PortType = PortType.Data,
                DataType = connectedType.GenerateTypeHandle(Stencil),
            };
            call.OnConnection(parameterPort, p);
            if (expectedOutputType != typeof(void))
                Assert.That(call.OutputPort.DataType, Is.EqualTo(expectedOutputType.GenerateTypeHandle(Stencil)));
        }

        [TestCase(ClassType.Plain, "F", typeof(float), 0, typeof(float))]
        [TestCase(ClassType.Plain, "G", typeof(float), 0, typeof(float))]
        [TestCase(ClassType.Plain, "H", typeof(float), 0, typeof(float))]
        [TestCase(ClassType.Plain, "H", typeof(float), 1, typeof(float))]
        [TestCase(ClassType.Plain, "ListFromType", typeof(float), 0, typeof(float))]
        [TestCase(ClassType.Plain, "TypeFromList", typeof(List<float>), 0, typeof(List<float>))]
        [TestCase(ClassType.Plain, "ListFromList", typeof(List<float>), 0, typeof(List<float>))]

        [TestCase(ClassType.Generic, "F", typeof(float), 0, typeof(float))]
        [TestCase(ClassType.Generic, "G", typeof(float), 0, typeof(float))]
        [TestCase(ClassType.Generic, "H", typeof(float), 0, typeof(float))]
        [TestCase(ClassType.Generic, "H", typeof(float), 1, typeof(float))]
        [TestCase(ClassType.Generic, "ListFromType", typeof(float), 0, typeof(float))]
        [TestCase(ClassType.Generic, "TypeFromList", typeof(List<float>), 0, typeof(List<float>))]
        [TestCase(ClassType.Generic, "ListFromList", typeof(List<float>), 0, typeof(List<float>))]
        public void GenericInputTypeIsSolved(ClassType t, string methodName, Type connectedType, int methodParameterIndex,
            Type expectedInputType)
        {
            Type methodDeclaringType = t == ClassType.Plain ? typeof(C) : typeof(D<>);
            FunctionCallNodeModel call = GraphModel.CreateNode<FunctionCallNodeModel>("funcNode", Vector2.zero);
            call.AssetModel = GraphModel.AssetModel;
            call.MethodInfo = methodDeclaringType.GetMethod(methodName);
            Assert.NotNull(call.MethodInfo);
            var param = call.MethodInfo.GetParameters()[methodParameterIndex];
            call.DefineNode();
            var p = new PortModel
            {
                Direction = Direction.Output,
                PortType = PortType.Data,
                DataType = connectedType.GenerateTypeHandle(Stencil),
            };
            var parameterPort = call.GetPortForParameter(param.Name);
            call.OnConnection(parameterPort, p);
            Assert.That(parameterPort.DataType, Is.EqualTo(expectedInputType.GenerateTypeHandle(Stencil)));
        }

        protected override bool CreateGraphOnStartup => true;
    }
}
