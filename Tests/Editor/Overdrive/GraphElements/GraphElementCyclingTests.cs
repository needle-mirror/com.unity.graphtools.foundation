using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class GraphElementCyclingTests : GraphViewTester
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
            Store.State.RequestUIRebuild();
            yield return null;

            List<GraphElement> elemList = graphView.GraphElements.ToList().Where(e => e.IsSelectable() && !(e is Edge)).OrderBy(e => e.controlid).ToList();

            graphView.AddToSelection(elemList[0]);

            // Start at 1 since the 1st element has already been selected.
            for (int i = 1; i < k_NodeCount; i++)
            {
                helpers.KeyPressed(KeyCode.RightBracket);
                yield return null;
                Assert.AreEqual(1, graphView.Selection.Count);
                Assert.IsNotNull(elemList[i].Model);
                Assert.AreEqual(elemList[i].Model, (graphView.Selection[0] as IGraphElement)?.Model);
            }

            // Cycle one more brings us back to the 1st element
            helpers.KeyPressed(KeyCode.RightBracket);
            yield return null;

            Assert.AreEqual(1, graphView.Selection.Count);
            Assert.IsNotNull(elemList[0].Model);
            Assert.AreEqual(elemList[0].Model, (graphView.Selection[0] as IGraphElement)?.Model);
        }

        [UnityTest]
        public IEnumerator ElementCanBeCycledBackward()
        {
            Store.State.RequestUIRebuild();
            yield return null;

            List<GraphElement> elemList = graphView.GraphElements.ToList().Where(e => e.IsSelectable() && !(e is Edge)).OrderBy(e => e.controlid).ToList();

            graphView.AddToSelection(elemList[k_NodeCount - 1]);

            // Start at k_PresenterCount-2 since the last element (aka k_PresenterCount-1) has already been selected.
            for (int i = k_NodeCount - 2; i >= 0; i--)
            {
                helpers.KeyPressed(KeyCode.LeftBracket);
                yield return null;
                Assert.AreEqual(1, graphView.Selection.Count);
                Assert.IsNotNull(elemList[i].Model);
                Assert.AreEqual(elemList[i].Model, (graphView.Selection[0] as IGraphElement)?.Model);
            }

            // Cycle one more brings us back to the last element
            helpers.KeyPressed(KeyCode.LeftBracket);
            yield return null;

            Assert.AreEqual(1, graphView.Selection.Count);
            Assert.IsNotNull(elemList[k_NodeCount - 1].Model);
            Assert.AreEqual(elemList[k_NodeCount - 1].Model, (graphView.Selection[0] as IGraphElement)?.Model);
        }
    }
}
