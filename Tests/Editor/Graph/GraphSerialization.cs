using System;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Graph
{
    class TestGraph : IGraphTemplate
    {
        public Type StencilType => typeof(ClassStencil);
        public string GraphTypeName => "Test Graph";
        public string DefaultAssetName => "testgraph";

        public void InitBasicGraph(IGraphModel graph)
        {
            AssetDatabase.SaveAssets();
        }
    }

    class GraphSerialization : BaseFixture
    {
        protected override bool CreateGraphOnStartup => false;

        [Test]
        public void LoadGraphCommandLoadsCorrectGraph()
        {
            GraphAssetCreationHelpers<TestGraphAssetModel>.CreateGraphAsset(typeof(ClassStencil), "test", k_GraphPath);
            AssumeIntegrity();

            AssetDatabase.SaveAssets();
            Resources.UnloadAsset(m_CommandDispatcher.State.WindowState.AssetModel as Object);
            m_CommandDispatcher.Dispatch(new LoadGraphAssetCommand(k_GraphPath, 0, null));
            Assert.AreEqual(k_GraphPath, AssetDatabase.GetAssetPath((Object)GraphModel.AssetModel));
            AssertIntegrity();

            AssetDatabase.DeleteAsset(k_GraphPath);
        }

        [Test]
        public void CreateGraphAssetBuildsValidGraphModel()
        {
            GraphAssetCreationHelpers<TestGraphAssetModel>.CreateInMemoryGraphAsset(typeof(ClassStencil), "test");
            AssumeIntegrity();
        }

        [Test]
        public void CreateGraphAssetWithTemplateBuildsValidGraphModel()
        {
            var graphTemplate = new TestGraph();
            GraphAssetCreationHelpers<TestGraphAssetModel>.CreateInMemoryGraphAsset(typeof(ClassStencil), graphTemplate.DefaultAssetName, graphTemplate);
            AssertIntegrity();
        }

        [Test]
        public void CreateTestGraphCanBeReloaded()
        {
            var graphTemplate = new TestGraph();
            GraphAssetCreationHelpers<TestGraphAssetModel>.CreateGraphAsset(typeof(ClassStencil), graphTemplate.DefaultAssetName, k_GraphPath, graphTemplate);

            GraphModel graph = AssetDatabase.LoadAssetAtPath<GraphAssetModel>(k_GraphPath)?.GraphModel as GraphModel;
            Resources.UnloadAsset((Object)graph?.AssetModel);
            m_CommandDispatcher.Dispatch(new LoadGraphAssetCommand(k_GraphPath, 0, null));

            AssertIntegrity();

            AssetDatabase.DeleteAsset(k_GraphPath);
        }

        [Test]
        public void CreateTestGraphFromAssetModel()
        {
            var graphTemplate = new TestGraph();
            var assetModel = ScriptableObject.CreateInstance<TestGraphAssetModel>();
            var doCreateAction = ScriptableObject.CreateInstance<DoCreateAsset>();
            doCreateAction.SetUp(null, assetModel, graphTemplate);
            doCreateAction.CreateAndLoadAsset(k_GraphPath);
            AssertIntegrity();
        }

        [Test]
        public void GetAssetPathOnSubAssetDoesNotLoadMainAsset()
        {
            // Create asset file with two graph assets in it.
            GraphAssetCreationHelpers<TestGraphAssetModel>.CreateGraphAsset(typeof(ClassStencil), "test", k_GraphPath);
            var subAsset = GraphAssetCreationHelpers<OtherTestGraphAssetModel>.CreateGraphAsset(typeof(ClassStencil), "otherTest", null);
            AssetDatabase.AddObjectToAsset(subAsset as Object, k_GraphPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(subAsset as Object, out var guid, out long localId);
            Resources.UnloadAsset(m_CommandDispatcher.State.WindowState.AssetModel as Object);

            // Load the second asset.
            m_CommandDispatcher.Dispatch(new LoadGraphAssetCommand(k_GraphPath, localId, null));

            // Check that we loaded the second asset.
            Assert.AreEqual(guid, m_CommandDispatcher.State.WindowState.CurrentGraph.GraphModelAssetGUID);
            Assert.AreEqual(localId, m_CommandDispatcher.State.WindowState.CurrentGraph.AssetLocalId);

            // Call GetGraphAssetModelPath(), which was reloading the wrong asset (GTF-350).
            m_CommandDispatcher.State.WindowState.CurrentGraph.GetGraphAssetModelPath();
            Assert.AreEqual(subAsset, m_CommandDispatcher.State.WindowState.CurrentGraph.m_GraphAssetModel);

            // Call GetGraphAssetModel(), which was reloading the wrong asset (GTF-350).
            m_CommandDispatcher.State.WindowState.CurrentGraph.GetGraphAssetModel();
            Assert.AreEqual(subAsset, m_CommandDispatcher.State.WindowState.CurrentGraph.m_GraphAssetModel);

            AssetDatabase.DeleteAsset(k_GraphPath);
        }
    }
}
