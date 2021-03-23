using System;
using NUnit.Framework;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace UnityEditor.VisualScriptingTests.Actions
{
    [Category("Node")]
    [Category("Action")]
    class ReturnNodeTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        [Test]
        public void MovingAReturnNodeToAnotherFunctionShouldUpdateItsValuePortType([Values] TestingMode mode)
        {
            var a = GraphModel.CreateFunction("A", Vector2.left);
            a.ReturnType = TypeHandle.Float;
            var ret = a.CreateStackedNode<ReturnNodeModel>();

            var b = GraphModel.CreateFunction("B", Vector2.left);
            b.ReturnType = TypeHandle.Bool;

            TestPrereqActionPostreq(mode, () =>
            {
                RefreshReference(ref a);
                RefreshReference(ref b);
                RefreshReference(ref ret);
                Assert.That(ret.InputPort, Is.Not.Null);
                Assert.That(ret.InputPort.DataType, Is.EqualTo(TypeHandle.Float));
                return new MoveStackedNodesAction(new[] { ret }, b, 0);
            }, () =>
                {
                    RefreshReference(ref a);
                    RefreshReference(ref b);
                    RefreshReference(ref ret);
                    Assert.That(ret.InputPort, Is.Not.Null);
                    Assert.That(ret.InputPort.DataType, Is.EqualTo(TypeHandle.Bool));
                });
        }

        [Test]
        public void MovingAReturnNodeToAVoidFunctionShouldRemoveItsValuePortType([Values] TestingMode mode)
        {
            var a = GraphModel.CreateFunction("A", Vector2.left);
            a.ReturnType = TypeHandle.Float;
            var ret = a.CreateStackedNode<ReturnNodeModel>();

            var b = GraphModel.CreateFunction("B", Vector2.left);
            b.ReturnType = TypeHandle.Void;

            TestPrereqActionPostreq(mode, () =>
            {
                RefreshReference(ref a);
                RefreshReference(ref b);
                RefreshReference(ref ret);
                Assert.That(ret.InputPort, Is.Not.Null);
                Assert.That(ret.InputPort.DataType, Is.EqualTo(TypeHandle.Float));
                return new MoveStackedNodesAction(new[] { ret }, b, 0);
            }, () =>
                {
                    RefreshReference(ref a);
                    RefreshReference(ref b);
                    RefreshReference(ref ret);
                    Assert.That(ret.InputPort, Is.Null);
                });
        }

        [Test]
        public void ChangingAFunctionReturnTypeShouldUpdateItsReturnNodeValueType([Values] TestingMode mode)
        {
            var a = GraphModel.CreateFunction("A", Vector2.left);
            a.ReturnType = TypeHandle.Float;
            var ret = a.CreateStackedNode<ReturnNodeModel>();

            TestPrereqActionPostreq(mode, () =>
            {
                RefreshReference(ref a);
                RefreshReference(ref ret);
                Assert.That(ret.InputPort, Is.Not.Null);
                Assert.That(ret.InputPort.DataType, Is.EqualTo(TypeHandle.Float));
                return new UpdateFunctionReturnTypeAction(a, TypeHandle.Bool);
            }, () =>
                {
                    RefreshReference(ref a);
                    RefreshReference(ref ret);
                    Assert.That(ret.InputPort, Is.Not.Null);
                    Assert.That(ret.InputPort.DataType, Is.EqualTo(TypeHandle.Bool));
                });
        }

        [Test]
        public void ChangingAFunctionReturnTypeToVoidShouldRemoveItsReturnNodeValueType([Values] TestingMode mode)
        {
            var a = GraphModel.CreateFunction("A", Vector2.left);
            a.ReturnType = TypeHandle.Float;
            var ret = a.CreateStackedNode<ReturnNodeModel>();

            TestPrereqActionPostreq(mode, () =>
            {
                RefreshReference(ref a);
                RefreshReference(ref ret);
                Assert.That(ret.InputPort, Is.Not.Null);
                Assert.That(ret.InputPort.DataType, Is.EqualTo(TypeHandle.Float));
                return new UpdateFunctionReturnTypeAction(a, TypeHandle.Void);
            }, () =>
                {
                    RefreshReference(ref a);
                    RefreshReference(ref ret);
                    Assert.That(ret.InputPort, Is.Null);
                });
        }
    }
}
