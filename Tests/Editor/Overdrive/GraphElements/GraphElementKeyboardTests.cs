using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements.Utilities;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    public class GraphElementKeyboardTests : GraphViewTester
    {
        BasicNodeModel m_Node1Model;
        BasicNodeModel m_Node2Model;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_Node1Model = CreateNode("Node 1", new Vector2(20, 0));
            m_Node2Model = CreateNode("Node 2", new Vector2(250, 230));
        }

        [UnityTest]
        public IEnumerator ShortcutsWork()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            var node1 = m_Node1Model.GetUI<Node>(graphView);
            var node2 = m_Node2Model.GetUI<Node>(graphView);

            Assert.IsNotNull(node1);
            Assert.IsNotNull(node2);

            // TODO Do we really want to validate that pressing some key did the "right thing" by checking
            // transformation matrices? Seems it's not testing the right thing in a "keyboard tests". I'd
            // rather check that pressing "A" (for example) invokes GraphView.FrameAll. The check that "FrameAll"
            // pans and zooms as expected shoudld be the suibject of another test suite that isn't keyboard related.
            // (and that could actually be more "real" unit tests, not requiring inputs of any kind).

            VisualElement vc = graphView.contentViewContainer;
            Matrix4x4 transform = vc.transform.matrix;

            Assert.AreEqual(Matrix4x4.identity, vc.transform.matrix);

            // Select first element
            Vector3 originalNode1GlobalCenter = node1.LocalToWorld(node1.layout.center);
            helpers.MouseClickEvent(originalNode1GlobalCenter);
            yield return null;

            Assert.True(node1.selected);
            Assert.False(node2.selected);

            // Frame selection
            bool frameSelectedCommandIsValid = helpers.ValidateCommand("FrameSelected");
            yield return null;

            Assert.True(frameSelectedCommandIsValid);
            helpers.ExecuteCommand("FrameSelected");
            yield return null;

            Vector3 currentNode1GlobalCenter = node1.LocalToWorld(node1.layout.center);
            transform *= Matrix4x4.Translate(currentNode1GlobalCenter - originalNode1GlobalCenter);
            Assert.AreEqual(transform, vc.transform.matrix);

            // Frame all
            helpers.KeyPressed(KeyCode.A);
            yield return null;

            // (-115, -115) is the difference between originalNode1GlobalCenter and the center of the box
            // that bounds node1 and node2
            transform *= Matrix4x4.Translate(new Vector3(-115, -115, 0));
            Assert.AreEqual(transform, vc.transform.matrix);

            // Reset view to origin
            window.SendEvent(new Event {type = EventType.KeyDown, character = 'o', keyCode = (KeyCode)'o'});
            yield return null;

            window.SendEvent(new Event {type = EventType.KeyUp, character = 'o', keyCode = (KeyCode)'o'});
            yield return null;

            Assert.AreEqual(Matrix4x4.identity, vc.transform.matrix);

            // Select next
            window.SendEvent(new Event {type = EventType.KeyDown, character = ']', keyCode = (KeyCode)']' });
            yield return null;

            window.SendEvent(new Event {type = EventType.KeyUp, character = ']', keyCode = (KeyCode)']' });
            yield return null;

            Assert.False(node1.selected);
            Assert.True(node2.selected);

            yield return null;
        }
    }
}
