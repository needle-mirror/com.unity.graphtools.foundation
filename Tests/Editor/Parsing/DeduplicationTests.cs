using System;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEditor.VisualScripting.Model.Translators;
using UnityEngine;
using UnityEngine.VisualScripting;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Parsing
{
    class BaseDeduplicationLoopTestsFixture : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        protected void JoinStacks(StackBaseModel a, StackBaseModel b, string newStackName, out StackBaseModel newJoinStack)
        {
            newJoinStack = GraphModel.CreateStack(newStackName, Vector2.zero);
            GraphModel.CreateEdge(newJoinStack.InputPorts[0], a.OutputPorts[0]);
            GraphModel.CreateEdge(newJoinStack.InputPorts[0], b.OutputPorts[0]);
        }

        protected void CreateIfThenElseStacks(StackBaseModel ifStack, string thenName, string elseName, out StackBaseModel thenStack, out StackBaseModel elseStack)
        {
            var ifNode = ifStack.CreateStackedNode<IfConditionNodeModel>("if");

            if (thenName != null)
            {
                thenStack = GraphModel.CreateStack(thenName, Vector2.left);
                GraphModel.CreateEdge(thenStack.InputPorts[0], ifNode.ThenPort);
            }
            else
                thenStack = null;

            if (elseName != null)
            {
                elseStack = GraphModel.CreateStack(elseName, Vector2.right);
                GraphModel.CreateEdge(elseStack.InputPorts[0], ifNode.ElsePort);
            }
            else
                elseStack = null;
        }
    }

    [SuppressMessage("ReSharper", "InlineOutVariableDeclaration")]
    class DeduplicationTests : BaseDeduplicationLoopTestsFixture
    {
        // A
        //B C
        // D
        [Test]
        public void SimpleIf()
        {
            var a = GraphModel.CreateFunction("A", Vector2.zero);

            StackBaseModel b, c, d;
            CreateIfThenElseStacks(a, "b", "c", out b, out c);
            JoinStacks(b, c, "d", out d);

            Assert.That(RoslynTranslator.FindCommonDescendant(a, b, c), Is.EqualTo(d));
        }

        //       A
        //      / \
        //     B   C
        //     |  / \
        //     |  D  E
        //      \ | /
        //       \|/
        //        F
        [Test]
        public void ThreeWayIf()
        {
            var a = GraphModel.CreateFunction("A", Vector2.zero);

            StackBaseModel b, c, d, e, f;
            CreateIfThenElseStacks(a, "b", "c", out b, out c);
            CreateIfThenElseStacks(c, "d", "e", out d, out e);
            JoinStacks(d, e, "f", out f);
            GraphModel.CreateEdge(f.InputPorts[0], b.OutputPorts[0]);

            Assert.That(RoslynTranslator.FindCommonDescendant(a, d, e), Is.EqualTo(f));
            Assert.That(RoslynTranslator.FindCommonDescendant(a, b, c), Is.EqualTo(f));
        }

        //        A
        //      /   \
        //     B     C
        //    / \   / \
        //   D   E F   G
        //    \ /   \ /
        //     H     I
        //      \   /
        //        J
        [Test]
        public void TwoLevelIfs()
        {
            var a = GraphModel.CreateFunction("A", Vector2.zero);
            StackBaseModel b, c, d, e, f, g, h, i, j;
            CreateIfThenElseStacks(a, "b", "c", out b, out c);
            CreateIfThenElseStacks(b, "d", "e", out d, out e);
            CreateIfThenElseStacks(c, "f", "g", out f, out g);
            JoinStacks(d, e, "h", out h);
            JoinStacks(f, g, "i", out i);
            JoinStacks(h, i, "h", out j);

            Assert.That(RoslynTranslator.FindCommonDescendant(a, b, c), Is.EqualTo(j));
            Assert.That(RoslynTranslator.FindCommonDescendant(a, d, e), Is.EqualTo(h));
            Assert.That(RoslynTranslator.FindCommonDescendant(a, f, g), Is.EqualTo(i));
            Assert.That(RoslynTranslator.FindCommonDescendant(a, d, f), Is.EqualTo(j));
        }

        //        A
        //      /   \
        //     B     C
        //    / \   / \
        //   D   E F   G
        //    \  | |  /
        //     \ | | /
        //      \| |/
        //        H
        [Test]
        public void FourWayJoin()
        {
            var a = GraphModel.CreateFunction("A", Vector2.zero);
            StackBaseModel b, c, d, e, f, g, h;
            CreateIfThenElseStacks(a, "b", "c", out b, out c);
            CreateIfThenElseStacks(b, "d", "e", out d, out e);
            CreateIfThenElseStacks(c, "f", "g", out f, out g);
            JoinStacks(d, e, "h", out h);

            GraphModel.CreateEdge(h.InputPorts[0], f.OutputPorts[0]);
            GraphModel.CreateEdge(h.InputPorts[0], g.OutputPorts[0]);

            Assert.That(RoslynTranslator.FindCommonDescendant(a, b, c), Is.EqualTo(h));
            Assert.That(RoslynTranslator.FindCommonDescendant(a, d, e), Is.EqualTo(h));
            Assert.That(RoslynTranslator.FindCommonDescendant(a, f, g), Is.EqualTo(h));
            Assert.That(RoslynTranslator.FindCommonDescendant(a, d, f), Is.EqualTo(h));
        }

        //        A
        //      /   \
        //     |     C
        //      \   /
        //        B
        [Test]
        public void IfNoThen()
        {
            var a = GraphModel.CreateFunction("A", Vector2.zero);

            StackBaseModel b, c;
            CreateIfThenElseStacks(a, "b", "c", out b, out c);

            GraphModel.CreateEdge(b.InputPorts[0], c.OutputPorts[0]);

            Assert.That(RoslynTranslator.FindCommonDescendant(a, b, c), Is.EqualTo(b));
        }

        //        A
        //      /   \
        //     B     C
        [Test]
        public void UnjoinedIfHasNoCommonDescendant()
        {
            var a = GraphModel.CreateFunction("A", Vector2.zero);

            StackBaseModel b, c;
            CreateIfThenElseStacks(a, "b", "c", out b, out c);
            Assert.That(RoslynTranslator.FindCommonDescendant(a, b, c), Is.Null);
        }

        //        A
        //      /   \
        //     |     C
        //      \   / \
        //        B
        [Test]
        public void IfNoThenElseIfNoThen()
        {
            var a = GraphModel.CreateFunction("A", Vector2.zero);

            StackBaseModel b, c;
            CreateIfThenElseStacks(a, "b", "c", out b, out c);
            b.CreateStackedNode<Type0FakeNodeModel>("b");
            c.CreateStackedNode<Type0FakeNodeModel>("c");

            var cIfNode = c.CreateStackedNode<IfConditionNodeModel>("if_c");

            GraphModel.CreateEdge(b.InputPorts[0], cIfNode.ThenPort);

            // as C has an if node with a disconnect else branch, B cannot be a descendant of both branches
            // so common(b,c) should return null
            Assert.That(RoslynTranslator.FindCommonDescendant(a, b, c), Is.Null);
        }

        //       A
        //      / \
        //     |   C
        //      \ / \
        //       B   D
        //        \ /
        //         E
        [Test]
        public void NestedIfs()
        {
            var a = GraphModel.CreateFunction("A", Vector2.zero);

            StackBaseModel b, c;
            CreateIfThenElseStacks(a, "b", "c", out b, out c);
            var d = GraphModel.CreateStack("d", Vector2.left);
            var e = GraphModel.CreateStack("e", Vector2.left);

            b.CreateStackedNode<Type0FakeNodeModel>("b");
            c.CreateStackedNode<Type0FakeNodeModel>("c");
            d.CreateStackedNode<Type0FakeNodeModel>("d");
            e.CreateStackedNode<Type0FakeNodeModel>("e");

            var cIfNode = c.CreateStackedNode<IfConditionNodeModel>("if_c");

            GraphModel.CreateEdge(b.InputPorts[0], cIfNode.ThenPort);
            GraphModel.CreateEdge(d.InputPorts[0], cIfNode.ElsePort);

            GraphModel.CreateEdge(e.InputPorts[0], b.OutputPorts[0]);
            GraphModel.CreateEdge(e.InputPorts[0], d.OutputPorts[0]);

            // as C has an if node, a common descendant of (C,X) must be a descendant of (B,D,X), here E
            Assert.That(RoslynTranslator.FindCommonDescendant(a, a, c), Is.EqualTo(e));
            Assert.That(RoslynTranslator.FindCommonDescendant(a, b, c), Is.EqualTo(e));
        }

        //        A
        //      /   \
        //     B     |
        //      \   /
        //        C
        [Test]
        public void IfNoElse()
        {
            var a = GraphModel.CreateFunction("A", Vector2.zero);


            StackBaseModel b, c;
            CreateIfThenElseStacks(a, "b", "c", out b, out c);

            GraphModel.CreateEdge(c.InputPorts[0], b.OutputPorts[0]);

            Assert.That(RoslynTranslator.FindCommonDescendant(a, b, c), Is.EqualTo(c));
        }

        //        A
        //      /   \
        //     B     C
        //    / \   / \
        //   D   E F   G
        //    \  | |  /
        //     \ |/  /
        //       H  /
        //       \ /
        //        I
        [Test]
        public void WickedThreeWayJoin()
        {
            var a = GraphModel.CreateFunction("A", Vector2.zero);
            StackBaseModel b, c, d, e, f, g, h, i;
            CreateIfThenElseStacks(a, "b", "c", out b, out c);
            CreateIfThenElseStacks(b, "d", "e", out d, out e);
            CreateIfThenElseStacks(c, "f", "g", out f, out g);
            JoinStacks(d, e, "h", out h);

            GraphModel.CreateEdge(h.InputPorts[0], f.OutputPorts[0]);
            JoinStacks(h, g, "i", out i);

            Assert.That(RoslynTranslator.FindCommonDescendant(a, b, c), Is.EqualTo(i));
            Assert.That(RoslynTranslator.FindCommonDescendant(a, h, g), Is.EqualTo(i));
            Assert.That(RoslynTranslator.FindCommonDescendant(a, d, e), Is.EqualTo(h));
            Assert.That(RoslynTranslator.FindCommonDescendant(a, d, f), Is.EqualTo(h));
            Assert.That(RoslynTranslator.FindCommonDescendant(a, e, f), Is.EqualTo(h));
            Assert.That(RoslynTranslator.FindCommonDescendant(a, b, g), Is.EqualTo(i));
        }
    }
}
