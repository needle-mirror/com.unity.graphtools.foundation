using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.Editor.Highlighting;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using Port = UnityEditor.VisualScripting.Editor.Port;

namespace UnityEditor.VisualScriptingTests.Misc
{
    [TestFixture]
    sealed class HighlightHelperTest
    {
        VseWindow m_Window;
        Stencil m_Stencil;
        Port m_Port;
        BlackboardVariableField m_IntField;
        BlackboardVariableField m_StringField;
        VariableNodeModel m_IntTokenModel;
        VariableNodeModel m_StringTokenModel;

        [SetUp]
        public void SetUp()
        {
            m_Window = EditorWindow.GetWindowWithRect<VseWindow>(new Rect(Vector2.zero, new Vector2(800, 600)));
            m_Stencil = new ClassStencil();

            var assetModel = ScriptableObject.CreateInstance<VSGraphAssetModel>();
            var graphModel = assetModel.CreateGraph<VSGraphModel>("test", typeof(ClassStencil), false);

            var portModelMock = new Mock<IPortModel>();
            portModelMock.Setup(x => x.GraphModel).Returns(graphModel);
            portModelMock.Setup(x => x.PortType).Returns(PortType.Event);
            portModelMock.Setup(x => x.Direction).Returns(Direction.Input);
            portModelMock.Setup(x => x.Name).Returns("port");
            portModelMock.Setup(x => x.DataType).Returns(typeof(float).GenerateTypeHandle(m_Stencil));
            m_Port = Port.Create(portModelMock.Object, m_Window.Store, Orientation.Horizontal);

            VariableDeclarationModel intVariableModel = graphModel.CreateGraphVariableDeclaration(
                "int", typeof(int).GenerateTypeHandle(m_Stencil), false
            );
            VariableDeclarationModel stringVariableModel = graphModel.CreateGraphVariableDeclaration(
                "string", typeof(string).GenerateTypeHandle(m_Stencil), false
            );

            m_IntField = new BlackboardVariableField(m_Window.Store, intVariableModel, m_Window.GraphView);
            m_StringField = new BlackboardVariableField(m_Window.Store, stringVariableModel, m_Window.GraphView);

            m_IntTokenModel = Activator.CreateInstance<VariableNodeModel>();
            m_IntTokenModel.DeclarationModel = intVariableModel;
            m_IntTokenModel.AssetModel = assetModel;

            m_StringTokenModel = Activator.CreateInstance<VariableNodeModel>();
            m_StringTokenModel.DeclarationModel = stringVariableModel;
            m_StringTokenModel.AssetModel = assetModel;
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Window != null)
            {
                m_Window.Close();
            }

            m_Stencil = null;
        }

        [Test]
        public void TestHighlightTokenSelection()
        {
            m_Window.GraphView.UIController.Blackboard.GraphVariables.AddRange(new List<IHighlightable>
                { m_IntField, m_StringField }
            );

            var intToken1 = new Token(m_IntTokenModel, m_Window.Store, m_Port, m_Port, m_Window.GraphView);
            var intToken2 = new Token(m_IntTokenModel, m_Window.Store, m_Port, m_Port, m_Window.GraphView);
            var stringToken1 = new Token(m_StringTokenModel, m_Window.Store, m_Port, m_Port, m_Window.GraphView);
            var stringToken2 = new Token(m_StringTokenModel, m_Window.Store, m_Port, m_Port, m_Window.GraphView);

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

            var intToken1 = new Token(m_IntTokenModel, m_Window.Store, m_Port, m_Port, m_Window.GraphView);
            var stringToken1 = new Token(m_StringTokenModel, m_Window.Store, m_Port, m_Port, m_Window.GraphView);

            m_Window.GraphView.AddElement(intToken1);
            m_Window.GraphView.AddElement(stringToken1);
            m_Window.GraphView.AddElement(m_IntField);
            m_Window.GraphView.AddElement(m_StringField);


            m_IntField.Select(m_Window.GraphView, false);

            Assert.IsTrue(intToken1.highlighted, "1. intToken1.highlighted");
            Assert.IsFalse(m_IntField.highlighted, "1. m_IntField.highlighted");
            Assert.IsFalse(stringToken1.highlighted, "1. stringToken1.highlighted");
            Assert.IsFalse(m_StringField.highlighted, "1. m_StringField.highlighted");

            m_StringField.Select(m_Window.GraphView, true);

            Assert.IsTrue(intToken1.highlighted, "2. intToken1.highlighted");
            Assert.IsFalse(m_IntField.highlighted, "2. m_IntField.highlighted");
            Assert.IsTrue(stringToken1.highlighted, "2. stringToken1.highlighted");
            Assert.IsFalse(m_StringField.highlighted, "2. m_StringField.highlighted");

            m_IntField.Unselect(m_Window.GraphView);

            Assert.IsFalse(intToken1.highlighted, "3. intToken1.highlighted");
            Assert.IsFalse(m_IntField.highlighted, "3. m_IntField.highlighted");
            Assert.IsTrue(stringToken1.highlighted, "3. stringToken1.highlighted");
            Assert.IsFalse(m_StringField.highlighted, "3. m_StringField.highlighted");

            m_StringField.Unselect(m_Window.GraphView);

            Assert.IsFalse(intToken1.highlighted, "4. intToken1.highlighted");
            Assert.IsFalse(m_IntField.highlighted, "4. m_IntField.highlighted");
            Assert.IsFalse(stringToken1.highlighted, "4. stringToken1.highlighted");
            Assert.IsFalse(m_StringField.highlighted, "4. m_StringField.highlighted");
        }
    }
}
