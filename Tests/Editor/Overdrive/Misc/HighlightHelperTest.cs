using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Misc
{
    [TestFixture]
    sealed class HighlightHelperTest
    {
        VseWindow m_Window;
        IGTFGraphAssetModel m_GraphAssetModel;
        BlackboardVariableField m_IntField;
        BlackboardVariableField m_StringField;
        VariableNodeModel m_IntTokenModel;
        VariableNodeModel m_StringTokenModel;

        [SetUp]
        public void SetUp()
        {
            m_Window = EditorWindow.GetWindowWithRect<VseWindow>(new Rect(Vector2.zero, new Vector2(800, 600)));

            m_GraphAssetModel = ScriptableObject.CreateInstance<TestGraphAssetModel>();
            m_GraphAssetModel.CreateGraph("test", typeof(ClassStencil), false);

            IGTFVariableDeclarationModel intVariableModel = m_GraphAssetModel.GraphModel.CreateGraphVariableDeclaration(
                "int", typeof(int).GenerateTypeHandle(), ModifierFlags.None, false
            );
            IGTFVariableDeclarationModel stringVariableModel = m_GraphAssetModel.GraphModel.CreateGraphVariableDeclaration(
                "string", typeof(string).GenerateTypeHandle(), ModifierFlags.None, false
            );

            m_IntField = new BlackboardVariableField(((GraphViewEditorWindow)m_Window).Store, intVariableModel, m_Window.GraphView);
            m_StringField = new BlackboardVariableField(((GraphViewEditorWindow)m_Window).Store, stringVariableModel, m_Window.GraphView);

            m_IntTokenModel = m_GraphAssetModel.GraphModel.CreateNode<VariableNodeModel>();
            m_IntTokenModel.DeclarationModel = intVariableModel;

            m_StringTokenModel = m_GraphAssetModel.GraphModel.CreateNode<VariableNodeModel>();
            m_StringTokenModel.DeclarationModel = stringVariableModel;
        }

        [TearDown]
        public void TearDown()
        {
            GraphElementFactory.RemoveAll(m_Window.GraphView);

            if (m_Window != null)
            {
                m_Window.Close();
            }
        }

        [Test]
        public void TestHighlightTokenSelection()
        {
            m_Window.GraphView.UIController.Blackboard.GraphVariables.AddRange(new List<IHighlightable>
                { m_IntField, m_StringField }
            );

            var intToken1 = GraphElementFactory.CreateUI<Token>(m_Window.GraphView, ((GraphViewEditorWindow)m_Window).Store, m_IntTokenModel);
            var intToken2 = GraphElementFactory.CreateUI<Token>(m_Window.GraphView, ((GraphViewEditorWindow)m_Window).Store, m_IntTokenModel);
            var stringToken1 = GraphElementFactory.CreateUI<Token>(m_Window.GraphView, ((GraphViewEditorWindow)m_Window).Store, m_StringTokenModel);
            var stringToken2 = GraphElementFactory.CreateUI<Token>(m_Window.GraphView, ((GraphViewEditorWindow)m_Window).Store, m_StringTokenModel);

            m_Window.GraphView.AddElement(intToken1);
            m_Window.GraphView.AddElement(intToken2);
            m_Window.GraphView.AddElement(stringToken1);
            m_Window.GraphView.AddElement(stringToken2);

            intToken1.Select(m_Window.GraphView, false);
            m_Window.GraphView.HighlightGraphElements();

            Assert.IsFalse(intToken1.Highlighted, "1. intToken1.highlighted");
            Assert.IsTrue(intToken2.Highlighted, "1. intToken2.highlighted");
            Assert.IsTrue(m_IntField.Highlighted, "1. m_IntField.highlighted");
            Assert.IsFalse(stringToken1.Highlighted, "1. stringToken1.highlighted");
            Assert.IsFalse(stringToken2.Highlighted, "1. stringToken2.highlighted");
            Assert.IsFalse(m_StringField.Highlighted, "1. m_StringField.highlighted");

            intToken1.Unselect(m_Window.GraphView);
            m_Window.GraphView.HighlightGraphElements();

            Assert.IsFalse(intToken1.Highlighted, "2. intToken1.highlighted");
            Assert.IsFalse(intToken2.Highlighted, "2. intToken2.highlighted");
            Assert.IsFalse(m_IntField.Highlighted, "2. m_IntField.highlighted");
            Assert.IsFalse(stringToken1.Highlighted, "2. stringToken1.highlighted");
            Assert.IsFalse(stringToken2.Highlighted, "2. stringToken2.highlighted");
            Assert.IsFalse(m_StringField.Highlighted, "2. m_StringField.highlighted");
        }

        [Test]
        public void TestHighlightFieldSelection()
        {
            m_Window.GraphView.UIController.Blackboard.GraphVariables.AddRange(new List<IHighlightable>
                { m_IntField, m_StringField }
            );

            var intToken1 = GraphElementFactory.CreateUI<Token>(m_Window.GraphView, ((GraphViewEditorWindow)m_Window).Store, m_IntTokenModel);
            var stringToken1 = GraphElementFactory.CreateUI<Token>(m_Window.GraphView, ((GraphViewEditorWindow)m_Window).Store, m_StringTokenModel);

            m_Window.GraphView.AddElement(intToken1);
            m_Window.GraphView.AddElement(stringToken1);
            m_Window.GraphView.AddElement(m_IntField);
            m_Window.GraphView.AddElement(m_StringField);


            m_IntField.Select(m_Window.GraphView, false);

            Assert.IsTrue(intToken1.Highlighted, "1. intToken1.highlighted");
            Assert.IsFalse(m_IntField.highlighted, "1. m_IntField.highlighted");
            Assert.IsFalse(stringToken1.Highlighted, "1. stringToken1.highlighted");
            Assert.IsFalse(m_StringField.highlighted, "1. m_StringField.highlighted");

            m_StringField.Select(m_Window.GraphView, true);

            Assert.IsTrue(intToken1.Highlighted, "2. intToken1.highlighted");
            Assert.IsFalse(m_IntField.highlighted, "2. m_IntField.highlighted");
            Assert.IsTrue(stringToken1.Highlighted, "2. stringToken1.highlighted");
            Assert.IsFalse(m_StringField.highlighted, "2. m_StringField.highlighted");

            m_IntField.Unselect(m_Window.GraphView);

            Assert.IsFalse(intToken1.Highlighted, "3. intToken1.highlighted");
            Assert.IsFalse(m_IntField.highlighted, "3. m_IntField.highlighted");
            Assert.IsTrue(stringToken1.Highlighted, "3. stringToken1.highlighted");
            Assert.IsFalse(m_StringField.highlighted, "3. m_StringField.highlighted");

            m_StringField.Unselect(m_Window.GraphView);

            Assert.IsFalse(intToken1.Highlighted, "4. intToken1.highlighted");
            Assert.IsFalse(m_IntField.highlighted, "4. m_IntField.highlighted");
            Assert.IsFalse(stringToken1.Highlighted, "4. stringToken1.highlighted");
            Assert.IsFalse(m_StringField.highlighted, "4. m_StringField.highlighted");
        }
    }
}
