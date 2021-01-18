using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class GraphViewDelegatesTests : GraphViewTester
    {
        class TestGraphElement : GraphElement
        {
            public TestGraphElement(GraphView graphView)
            {
                GraphView = graphView;
                style.backgroundColor = Color.red;
                MinimapColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            }

            public override bool IsResizable() => true;
        }

        [UnityTest]
        public IEnumerator ChangingGraphViewTransformExecutesViewTransformedDelegate()
        {
            bool viewTransformChanged = false;

            graphView.ViewTransformChangedCallback += elements => viewTransformChanged = true;

            graphView.UpdateViewTransform(new Vector3(10, 10, 10), new Vector3(10, 10));

            yield return null;
            Assert.IsTrue(viewTransformChanged);
        }

        [UnityTest]
        public IEnumerator ChangingZoomLevelExecutesViewTransformedDelegate()
        {
            bool viewTransformChanged = false;
            float minZoomScale = 0.1f;
            float maxZoomScale = 3;

            graphView.ViewTransformChangedCallback += elements => viewTransformChanged = true;
            graphView.SetupZoom(minZoomScale, maxZoomScale, 1.0f);
            yield return null;

            helpers.ScrollWheelEvent(10.0f, graphView.worldBound.center);
            yield return null;

            Assert.IsTrue(viewTransformChanged);
        }

        [UnityTest]
        public IEnumerator ChangingGraphViewTransformRoundsToPixelGrid()
        {
            var capturedPos = Vector3.zero;
            graphView.ViewTransformChangedCallback += graphView => capturedPos = graphView.contentViewContainer.transform.position;

            var pos = new Vector3(10.3f, 10.6f, 10.0f);
            graphView.UpdateViewTransform(pos, new Vector3(10, 10));

            yield return null;
            Assert.AreEqual(new Vector3(GraphViewStaticBridge.RoundToPixelGrid(pos.x), GraphViewStaticBridge.RoundToPixelGrid(pos.y), 10.0f), capturedPos);
        }
    }
}
