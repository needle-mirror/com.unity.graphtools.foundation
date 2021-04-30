using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.TestTools;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI
{
    class HighlightTests : BaseUIFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        IVariableDeclarationModel m_IntVariableModel;
        IVariableDeclarationModel m_StringVariableModel;

        VariableNodeModel m_IntTokenModel1;
        VariableNodeModel m_IntTokenModel2;
        VariableNodeModel m_StringTokenModel1;
        VariableNodeModel m_StringTokenModel2;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_IntVariableModel = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "int", ModifierFlags.None, false);

            m_StringVariableModel = GraphModel.CreateGraphVariableDeclaration(typeof(string).GenerateTypeHandle(), "string", ModifierFlags.None, false);

            m_IntTokenModel1 = GraphModel.CreateNode<VariableNodeModel>();
            m_IntTokenModel1.DeclarationModel = m_IntVariableModel;

            m_IntTokenModel2 = GraphModel.CreateNode<VariableNodeModel>();
            m_IntTokenModel2.DeclarationModel = m_IntVariableModel;

            m_StringTokenModel1 = GraphModel.CreateNode<VariableNodeModel>();
            m_StringTokenModel1.DeclarationModel = m_StringVariableModel;

            m_StringTokenModel2 = GraphModel.CreateNode<VariableNodeModel>();
            m_StringTokenModel2.DeclarationModel = m_StringVariableModel;
        }

        void GetUI(out TokenNode intToken1, out TokenNode intToken2, out TokenNode stringToken1, out TokenNode stringToken2,
            out IHighlightable intField, out IHighlightable stringField)
        {
            intToken1 = m_IntTokenModel1.GetUI<TokenNode>(GraphView);
            Assert.IsNotNull(intToken1);

            intToken2 = m_IntTokenModel2.GetUI<TokenNode>(GraphView);
            Assert.IsNotNull(intToken2);

            stringToken1 = m_StringTokenModel1.GetUI<TokenNode>(GraphView);
            Assert.IsNotNull(stringToken1);

            stringToken2 = m_StringTokenModel2.GetUI<TokenNode>(GraphView);
            Assert.IsNotNull(stringToken2);

            intField = null;
            stringField = null;

            intField = GraphView.GetBlackboard().Highlightables.FirstOrDefault(gv =>
                ReferenceEquals((gv as BlackboardField)?.Model, m_IntVariableModel));
            Assert.IsNotNull(intField);

            stringField = GraphView.GetBlackboard().Highlightables.FirstOrDefault(gv =>
                ReferenceEquals((gv as BlackboardField)?.Model, m_StringVariableModel));
            Assert.IsNotNull(stringField);
        }

        [UnityTest]
        public IEnumerator TestHighlightTokenSelection()
        {
            MarkGraphViewStateDirty();
            yield return null;

            GetUI(out TokenNode intToken1, out TokenNode intToken2, out TokenNode stringToken1, out TokenNode stringToken2,
                out IHighlightable intField, out IHighlightable stringField);

            CommandDispatcher.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, intToken1.Model));
            yield return null;

            Assert.IsFalse(intToken1.Highlighted, "1. intToken1.highlighted");
            Assert.IsTrue(intToken2.Highlighted, "1. intToken2.highlighted");
            Assert.IsTrue(intField.Highlighted, "1. m_IntField.highlighted");
            Assert.IsFalse(stringToken1.Highlighted, "1. stringToken1.highlighted");
            Assert.IsFalse(stringToken2.Highlighted, "1. stringToken2.highlighted");
            Assert.IsFalse(stringField.Highlighted, "1. m_StringField.highlighted");

            CommandDispatcher.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Remove, intToken1.Model));
            yield return null;

            Assert.IsFalse(intToken1.Highlighted, "2. intToken1.highlighted");
            Assert.IsFalse(intToken2.Highlighted, "2. intToken2.highlighted");
            Assert.IsFalse(intField.Highlighted, "2. m_IntField.highlighted");
            Assert.IsFalse(stringToken1.Highlighted, "2. stringToken1.highlighted");
            Assert.IsFalse(stringToken2.Highlighted, "2. stringToken2.highlighted");
            Assert.IsFalse(stringField.Highlighted, "2. m_StringField.highlighted");
        }

        [UnityTest]
        public IEnumerator TestHighlightFieldSelection()
        {
            MarkGraphViewStateDirty();
            yield return null;

            GetUI(out var intToken1, out _, out var stringToken1, out _,
                out var intField, out var stringField);

            CommandDispatcher.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, (intField as GraphElement)?.Model));
            yield return null;

            Assert.IsTrue(intToken1.Highlighted, "1. intToken1.highlighted");
            Assert.IsFalse(intField.Highlighted, "1. m_IntField.highlighted");
            Assert.IsFalse(stringToken1.Highlighted, "1. stringToken1.highlighted");
            Assert.IsFalse(stringField.Highlighted, "1. m_StringField.highlighted");

            CommandDispatcher.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, (stringField as GraphElement)?.Model));
            yield return null;

            Assert.IsTrue(intToken1.Highlighted, "2. intToken1.highlighted");
            Assert.IsFalse(intField.Highlighted, "2. m_IntField.highlighted");
            Assert.IsTrue(stringToken1.Highlighted, "2. stringToken1.highlighted");
            Assert.IsFalse(stringField.Highlighted, "2. m_StringField.highlighted");

            CommandDispatcher.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Remove, (intField as GraphElement)?.Model));
            yield return null;

            Assert.IsFalse(intToken1.Highlighted, "3. intToken1.highlighted");
            Assert.IsFalse(intField.Highlighted, "3. m_IntField.highlighted");
            Assert.IsTrue(stringToken1.Highlighted, "3. stringToken1.highlighted");
            Assert.IsFalse(stringField.Highlighted, "3. m_StringField.highlighted");

            CommandDispatcher.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Remove, (stringField as GraphElement)?.Model));
            yield return null;

            Assert.IsFalse(intToken1.Highlighted, "4. intToken1.highlighted");
            Assert.IsFalse(intField.Highlighted, "4. m_IntField.highlighted");
            Assert.IsFalse(stringToken1.Highlighted, "4. stringToken1.highlighted");
            Assert.IsFalse(stringField.Highlighted, "4. m_StringField.highlighted");
        }
    }
}
