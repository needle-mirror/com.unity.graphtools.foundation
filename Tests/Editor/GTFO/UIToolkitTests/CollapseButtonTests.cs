using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIToolkitTests
{
    public class CollapseButtonTests : BaseTestFixture
    {
        [Test]
        public void ChangeValueTriggersChangeCallback()
        {
            var collapseButton = new CollapseButton();
            bool called = false;
            collapseButton.RegisterCallback<ChangeEvent<bool>>(e => called = true);
            m_Window.rootVisualElement.Add(collapseButton);
            Assert.IsFalse(collapseButton.value);
            collapseButton.value = true;

            Assert.IsTrue(called, "CollapsedButton did not called our callback.");
        }

        [Test]
        public void SetValueWithoutNotifyDoesNotTriggerChangeCallback()
        {
            var collapseButton = new CollapseButton();
            bool called = false;
            collapseButton.RegisterCallback<ChangeEvent<bool>>(e => called = true);
            m_Window.rootVisualElement.Add(collapseButton);
            Assert.IsFalse(collapseButton.value);
            collapseButton.SetValueWithoutNotify(true);

            Assert.IsTrue(collapseButton.value, "CollapsedButton value did not change.");
            Assert.IsFalse(called, "CollapsedButton called our callback.");
        }

        [UnityTest]
        public IEnumerator ClickingButtonIconChangesItsValue()
        {
            var collapseButton = new CollapseButton();
            m_Window.rootVisualElement.Add(collapseButton);
            // Do layout
            yield return null;

            Assert.IsFalse(collapseButton.value);
            var icon = collapseButton.SafeQ("icon");
            Vector2 center = collapseButton.parent.LocalToWorld(collapseButton.layout.center);
            EventHelper.Click(center);

            Assert.IsTrue(collapseButton.value);
        }
    }
}
