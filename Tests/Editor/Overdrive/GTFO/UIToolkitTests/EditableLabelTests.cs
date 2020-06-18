using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.Helpers;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIElementsTests
{
    public class EditableLabelTests : BaseTestFixture
    {
        static readonly string k_SomeText = "Some text";

        [Test]
        public void SetValueWithoutNotifyDoesNotTriggerChangeCallback()
        {
            var editableLabel = new EditableLabel();
            bool called = false;
            editableLabel.RegisterCallback<ChangeEvent<string>>(e => called = true);
            m_Window.rootVisualElement.Add(editableLabel);
            editableLabel.SetValueWithoutNotify("Blah");

            Assert.IsFalse(called, "CollapsedButton called our callback.");
        }

        [UnityTest]
        public IEnumerator SingleClickOnEditableLabelDoesNotShowTextField()
        {
            var editableLabel = new EditableLabel();
            m_Window.rootVisualElement.Add(editableLabel);
            yield return null;

            var label = editableLabel.Q(EditableLabel.k_LabelName);
            var textField = editableLabel.Q(EditableLabel.k_TextFieldName);

            Assert.AreEqual(DisplayStyle.Flex, label.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, textField.resolvedStyle.display);

            var center = label.layout.center;
            Click(label, center);

            Assert.AreEqual(DisplayStyle.Flex, label.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, textField.resolvedStyle.display);
        }

        [UnityTest]
        public IEnumerator DoubleClickOnEditableLabelShowsTextField()
        {
            var editableLabel = new EditableLabel();
            m_Window.rootVisualElement.Add(editableLabel);
            yield return null;

            var label = editableLabel.Q(EditableLabel.k_LabelName);
            var textField = editableLabel.Q(EditableLabel.k_TextFieldName);
            var center = label.layout.center;

            Assert.AreEqual(DisplayStyle.Flex, label.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, textField.resolvedStyle.display);

            DoubleClick(label, center);

            Assert.AreEqual(DisplayStyle.None, label.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.Flex, textField.resolvedStyle.display);
        }

        [UnityTest]
        public IEnumerator EscapeCancelsEditing()
        {
            var editableLabel = new EditableLabel();
            editableLabel.SetValueWithoutNotify("My Text");
            string newValue = null;
            editableLabel.RegisterCallback<ChangeEvent<string>>(e => newValue = e.newValue);
            m_Window.rootVisualElement.Add(editableLabel);
            // Compute layout
            yield return null;

            var label = editableLabel.Q(EditableLabel.k_LabelName);
            var textField = editableLabel.Q(EditableLabel.k_TextFieldName);
            var center = label.layout.center;

            // Activate text field
            DoubleClick(label, center);

            // Type some text
            Type(textField, k_SomeText);

            // Type Escape
            Type(textField, KeyCode.Escape);

            Assert.IsNull(newValue);
            Assert.AreEqual(DisplayStyle.Flex, label.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, textField.resolvedStyle.display);
        }

        [UnityTest]
        public IEnumerator ReturnCommitsEditing()
        {
            var editableLabel = new EditableLabel();
            editableLabel.SetValueWithoutNotify("My Text");
            string newValue = null;
            editableLabel.RegisterCallback<ChangeEvent<string>>(e => newValue = e.newValue);
            m_Window.rootVisualElement.Add(editableLabel);
            // Compute layout
            yield return null;

            var label = editableLabel.Q(EditableLabel.k_LabelName);
            var textField = editableLabel.Q(EditableLabel.k_TextFieldName);
            var center = label.layout.center;

            // Activate text field
            DoubleClick(label, center);

            // Type some text
            Type(textField, k_SomeText);

            Type(textField, KeyCode.Return);

            Assert.AreEqual(k_SomeText, newValue);
        }

        [UnityTest]
        public IEnumerator BlurCommitsEditing()
        {
            var editableLabel = new EditableLabel();
            editableLabel.SetValueWithoutNotify("My Text");
            string newValue = null;
            editableLabel.RegisterCallback<ChangeEvent<string>>(e => newValue = e.newValue);
            m_Window.rootVisualElement.Add(editableLabel);
            // Compute layout
            yield return null;

            var label = editableLabel.Q(EditableLabel.k_LabelName);
            var textField = editableLabel.Q(EditableLabel.k_TextFieldName);
            var center = label.layout.center;

            // Activate text field
            DoubleClick(label, center);

            // Type some text
            Type(textField, k_SomeText);

            Click(m_Window.rootVisualElement, m_Window.rootVisualElement.layout.center);

            Assert.AreEqual(k_SomeText, newValue);
            Assert.AreEqual(DisplayStyle.Flex, label.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, textField.resolvedStyle.display);
        }
    }
}
