using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine.UIElements;
using UnityEngine.TestTools;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements.Utilities;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    public class GraphElementAttachTests : GraphViewTester
    {
        private static readonly Rect k_NodeRect = new Rect(SelectionDragger.k_PanAreaWidth * 2, SelectionDragger.k_PanAreaWidth * 3, 50, 50);

        Attacher CreateAttachedElement<T>(TestGraphViewWindow window) where T : VisualElement
        {
            T target = graphView.Q<T>();

            Attacher attacher = null;
            if (target != null)
            {
                VisualElement attached = new VisualElement();
                attached.SetLayout(new Rect(0, 0, 10, 10));
                attached.style.backgroundColor = Color.blue;

                target.parent.Add(attached);
                attacher = new Attacher(attached, target, SpriteAlignment.LeftCenter);
                attached.userData = attacher;
            }

            return attacher;
        }

        [UnityTest]
        public IEnumerator AttachedElementIsPlacedProperly()
        {
            // Create node.
            var node = new Node();
            node.SetPosition(k_NodeRect);
            node.style.width = k_NodeRect.width;
            node.style.height = k_NodeRect.height;

            graphView.AddElement(node);

            var attacher = CreateAttachedElement<Node>(window);
            Assert.AreNotEqual(null, attacher);

            // Move the movable node.
            helpers.MouseDownEvent(new Vector2(k_NodeRect.x + 5, k_NodeRect.y + 25));
            yield return null;

            helpers.MouseDragEvent(new Vector2(k_NodeRect.x + 5, k_NodeRect.y + 25),
                new Vector2(k_NodeRect.x + 15, k_NodeRect.y + 15));
            yield return null;

            helpers.MouseUpEvent(new Vector2(k_NodeRect.x + 15, k_NodeRect.y + 15));
            yield return null;

            Assert.AreEqual(attacher.target.layout.center.y, attacher.element.layout.center.y);
            Assert.AreNotEqual(attacher.target.layout.center.x, attacher.element.layout.center.x);

            yield return null;
        }
    }
}
