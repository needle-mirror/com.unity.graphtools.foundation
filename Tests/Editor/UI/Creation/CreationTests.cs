using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.UI
{
    class CreationTests : BaseUIFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        [UnityTest]
        public IEnumerator Test_CreateEmptyGraphClassStencil()
        {
            Assert.That(GetGraphElements().Count, Is.EqualTo(0));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_CreateStackWithNodes()
        {
            var stackModel = GraphModel.CreateStack(string.Empty, Vector2.zero);
            stackModel.CreateStackedNode<Type0FakeNodeModel>("Node0");
            stackModel.CreateStackedNode<Type1FakeNodeModel>("Node1");

            Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
            yield return null;

            Assert.That(GetGraphElements().Count, Is.EqualTo(3));
            Assert.That(GetGraphElement(0).Children().OfType<Node>().Count(), Is.EqualTo(2));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_AddNodeAtEndOfBranchTypeStackPutsItBeforeCondition()
        {
            var offset = new Vector2(300, 0);
            var stackModel = GraphModel.CreateStack(string.Empty, offset);
            stackModel.CreateStackedNode<Type0FakeNodeModel>("Node0");
            stackModel.CreateStackedNode<IfConditionNodeModel>("IfCondition", 0);

            Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
            yield return null;

            var stack = GetGraphElement(0);
            Assert.That(stack.Children().OfType<Node>().LastOrDefault() is IfConditionNode);

            // Position mouse above last stack separator (below IfCondition node)
            Vector2 position = new Vector2(offset.x + stack.layout.width / 2 - 15, offset.y + stack.layout.height - 6);
            Vector2 worldPosition = GraphView.contentViewContainer.LocalToWorld(position);
            Helpers.MouseMoveEvent(Vector2.zero, worldPosition);
            yield return null;

            //  Even though mouse position corresponds to insertion index == 2, make sure that
            // the actual index is 1 (above IfCondition node)
            var insertionIndex = ((StackNode)stack).GetInsertionIndex(position);
            Assert.That(insertionIndex, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Test_CreateEdgeBetweenStack()
        {
            var stackModel0 = GraphModel.CreateStack(string.Empty, new Vector2(-100, -100));
            var stackModel1 = GraphModel.CreateStack(string.Empty, new Vector2(100, 100));
            GraphModel.CreateEdge(stackModel1.InputPorts[0], stackModel0.OutputPorts[0]);

            Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
            yield return null;

            Assert.That(GetGraphElements().Count, Is.EqualTo(3));
            Assert.That(GraphView.edges.ToList().Count, Is.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_MandatoryQ_Throw()
        {
            GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);

            Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
            yield return null;

            Assert.That(GetGraphElements().Count, Is.EqualTo(1));
            Assert.Throws<MissingUIElementException>(() => GetGraphElement(0).MandatoryQ("bobette"));
            Assert.DoesNotThrow(() => GetGraphElement(0).MandatoryQ("title"));
            yield return null;
        }
    }
}
