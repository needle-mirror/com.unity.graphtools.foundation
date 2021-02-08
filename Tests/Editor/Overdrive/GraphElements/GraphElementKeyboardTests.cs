using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class GraphElementKeyboardTests : GraphViewTester
    {
        bool m_ShortcutCalled;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_ShortcutCalled = false;
            var shortcutsDictionary = new Dictionary<Event, ShortcutDelegate>();
            shortcutsDictionary.Add(Event.KeyboardEvent("A"), _ =>
            {
                m_ShortcutCalled = true;
                return ShortcutHandled.Handled;
            });
            graphView.ShortcutHandler = new ShortcutHandler(shortcutsDictionary);
        }

        [UnityTest]
        public IEnumerator ShortcutsWork()
        {
            CommandDispatcher.GraphToolState.RequestUIRebuild();
            yield return null;

            Assert.False(m_ShortcutCalled, "Shortcut was called without any key pressed.");

            graphView.Focus();
            window.SendEvent(new Event { type = EventType.KeyDown, character = 'a', keyCode = KeyCode.A });
            yield return null;

            Assert.True(m_ShortcutCalled, "Shortcut was not called.");
        }
    }
}
