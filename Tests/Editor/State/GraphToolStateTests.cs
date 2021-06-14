using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.CommandSystem
{
    public class GraphToolStateTests
    {
        IGraphAssetModel m_Asset1;
        IGraphAssetModel m_Asset2;

        [SetUp]
        public void SetUp()
        {
            m_Asset1 = GraphAssetCreationHelpers<TestGraphAssetModel>.CreateInMemoryGraphAsset(typeof(ClassStencil), "Test1");
            m_Asset2 = GraphAssetCreationHelpers<TestGraphAssetModel>.CreateInMemoryGraphAsset(typeof(ClassStencil), "Test2");
        }

        [Test]
        public void SelectionStateIsTiedToAssetAndView()
        {
            var node = new NodeModel();

            var viewGuid1 = SerializableGUID.Generate();
            var state = new GraphToolState(viewGuid1, null);
            state.LoadGraphAsset(m_Asset1, null);
            Assert.IsNotNull(state.SelectionState);
            using (var selectionUpdater = state.SelectionState.UpdateScope)
            {
                selectionUpdater.SelectElements(new[] { node }, true);
            }
            Assert.IsTrue(state.SelectionState.IsSelected(node));

            // Load another asset in the same view: node is not selected anymore.
            var state2 = new GraphToolState(viewGuid1, null);
            state2.LoadGraphAsset(m_Asset2, null);
            var selectionState2 = state2.SelectionState;
            Assert.IsNotNull(selectionState2);
            Assert.IsFalse(selectionState2.IsSelected(node));

            // Load the same asset in the another view: node is not selected anymore.
            var viewGuid2 = SerializableGUID.Generate();
            state2 = new GraphToolState(viewGuid2, null);
            state2.LoadGraphAsset(m_Asset1, null);
            selectionState2 = state2.SelectionState;
            Assert.IsNotNull(selectionState2);
            Assert.IsFalse(selectionState2.IsSelected(node));

            // Reload the same asset in the original view: node is still selected.
            state2 = new GraphToolState(viewGuid1, null);
            state2.LoadGraphAsset(m_Asset1, null);
            selectionState2 = state2.SelectionState;
            Assert.IsNotNull(selectionState2);
            Assert.IsTrue(selectionState2.IsSelected(node));
        }

        [Test]
        public void AllStateComponentsReturnsAllStateComponents()
        {
            var viewGuid1 = SerializableGUID.Generate();
            var state = new GraphToolState(viewGuid1, null);
            var sc = new HashSet<IStateComponent>(state.AllStateComponents);
            var foundComponents = new HashSet<IStateComponent>();
            foreach (var fieldInfo in typeof(GraphToolState)
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(fi => typeof(IStateComponent).IsAssignableFrom(fi.FieldType))
            )
            {
                foundComponents.Add(fieldInfo.GetValue(state) as IStateComponent);
                Assert.IsTrue(sc.Contains(fieldInfo.GetValue(state) as IStateComponent), fieldInfo.Name + " is not included in AllStateComponents");
            }

            Assert.AreEqual(sc, foundComponents);
        }
    }
}
