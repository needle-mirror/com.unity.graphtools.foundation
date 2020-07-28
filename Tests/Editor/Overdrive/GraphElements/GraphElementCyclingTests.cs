using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements.Utilities;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    public class GraphElementCyclingTests : GraphViewTester
    {
        const int k_NodeCount = 4;

        // TODO Don't check from teh keyboard. This is the Keyboard test job to check what is associated to what.
        // Here, check from "FrameNext/framePrev"

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            for (int i = 0; i < k_NodeCount; ++i)
            {
                CreateNode("", new Vector2(10 + 50 * i, 30));
            }
        }

        [UnityTest]
        public IEnumerator ElementCanBeCycledForward()
        {
            graphView.RebuildUI(GraphModel, Store);

            List<GraphElement> elemList = graphView.GraphElements.ToList().Where(e => e.IsSelectable() && !(e is Edge)).OrderBy(e => e.controlid).ToList();

            graphView.AddToSelection(elemList[0]);

            // Start at 1 since the 1st element has already been selected.
            for (int i = 1; i < k_NodeCount; i++)
            {
                helpers.KeyPressed(KeyCode.RightBracket);
                yield return null;
                Assert.AreEqual(1, graphView.Selection.Count);
                Assert.AreEqual(elemList[i], graphView.Selection[0]);
            }

            // Cycle one more brings us back to the 1st element
            helpers.KeyPressed(KeyCode.RightBracket);
            yield return null;

            Assert.AreEqual(1, graphView.Selection.Count);
            Assert.AreEqual(elemList[0], graphView.Selection[0]);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementCanBeCycledBackward()
        {
            graphView.RebuildUI(GraphModel, Store);

            List<GraphElement> elemList = graphView.GraphElements.ToList().Where(e => e.IsSelectable() && !(e is Edge)).OrderBy(e => e.controlid).ToList();

            graphView.AddToSelection(elemList[k_NodeCount - 1]);

            // Start at k_PresenterCount-2 since the last element (aka k_PresenterCount-1) has already been selected.
            for (int i = k_NodeCount - 2; i >= 0; i--)
            {
                helpers.KeyPressed(KeyCode.LeftBracket);
                yield return null;
                Assert.AreEqual(1, graphView.Selection.Count);
                Assert.AreEqual(elemList[i], graphView.Selection[0]);
            }

            // Cycle one more brings us back to the last element
            helpers.KeyPressed(KeyCode.LeftBracket);
            yield return null;

            Assert.AreEqual(1, graphView.Selection.Count);
            Assert.AreEqual(elemList[k_NodeCount - 1], graphView.Selection[0]);

            yield return null;
        }
    }
}
