using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class GraphElementResizeTests : GraphViewTester
    {
        TestGraphElement m_Element1;
        TestGraphElement m_Element2;

        readonly Rect k_Element1Position = new Rect(30, 30, 150, 150);
        readonly Rect k_Element2Position = new Rect(100, 190, 200, 200);

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            // Look ma, no models!

            m_Element1 = new TestGraphElement();
            m_Element1.SetPosition(k_Element1Position);
            m_Element1.style.width = k_Element1Position.width;
            m_Element1.style.height = k_Element1Position.height;
            m_Element1.hierarchy.Add(new Resizer());
            m_Element1.style.borderBottomWidth = 6;
            graphView.AddElement(m_Element1);

            m_Element2 = new TestGraphElement();
            m_Element2.SetPosition(k_Element2Position);
            m_Element2.style.width = k_Element2Position.width;
            m_Element2.style.height = k_Element2Position.height;
            m_Element1.hierarchy.Add(new Resizer());
            m_Element1.style.borderBottomWidth = 6;
            graphView.AddElement(m_Element2);
        }

        [UnityTest]
        public IEnumerator ResizableElementCanBeResized()
        {
            Resizer resizer = m_Element1.Q<Resizer>();
            Vector2 pickPoint = m_Element1.LocalToWorld(resizer.layout.center);
            Vector2 delta = new Vector2(10, -10);
            Vector2 destination = pickPoint + delta;

            // Resize the resizable element.
            helpers.DragTo(pickPoint, destination);
            yield return null;

            Assert.AreEqual(k_Element1Position.width + delta.x, m_Element1.layout.width);
            Assert.AreEqual(k_Element1Position.height + delta.y, m_Element1.layout.height);
        }

        [UnityTest]
        public IEnumerator ResizableElementCanBeResizedUnderOtherElement()
        {
            Resizer resizer = m_Element1.Q<Resizer>();
            Vector2 pickPoint = m_Element1.LocalToWorld(resizer.layout.center);
            Vector2 destination = graphView.LocalToWorld(m_Element2.layout.center);
            Vector2 delta = destination - pickPoint;

            // Resize the resizable element1 until the center of element2.
            helpers.DragTo(pickPoint, destination);
            yield return null;

            Assert.AreEqual(k_Element1Position.width + delta.x, m_Element1.layout.width);
            Assert.AreEqual(k_Element1Position.height + delta.y, m_Element1.layout.height);
        }
    }
}
