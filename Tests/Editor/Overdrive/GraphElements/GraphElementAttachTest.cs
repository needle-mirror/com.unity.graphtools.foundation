using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class GraphElementAttachTests : GraphViewTester
    {
        private static readonly Rect k_NodeRect = new Rect(SelectionDragger.panAreaWidth * 2, SelectionDragger.panAreaWidth * 3, 50, 50);

        Attacher CreateAttachedElement<T>(TestGraphViewWindow window) where T : VisualElement
        {
            T target = graphView.Q<T>();

            Attacher attacher = null;
            if (target != null)
            {
                VisualElement attached = new VisualElement
                {
                    style =
                    {
                        position = Position.Absolute,
                        bottom = 10,
                        right = 10,
                        backgroundColor = Color.blue
                    }
                };

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

            Assert.AreEqual(attacher.Target.layout.center.y, attacher.Element.layout.center.y);
            Assert.AreNotEqual(attacher.Target.layout.center.x, attacher.Element.layout.center.x);

            yield return null;
        }
    }
}
