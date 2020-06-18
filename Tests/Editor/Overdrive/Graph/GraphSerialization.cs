using System;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Graph
{
    class TestGraph : ICreatableGraphTemplate
    {
        public Type StencilType => typeof(ClassStencil);
        public bool ListInHomePage => false;
        public string GraphTypeName => "Test Graph";
        public string DefaultAssetName => "testgraph";

        public void InitBasicGraph(VSGraphModel graph)
        {
            AssetDatabase.SaveAssets();
        }
    }

    class GraphSerialization : BaseFixture
    {
        protected override bool CreateGraphOnStartup => false;

        [Test]
        public void LoadGraphActionLoadsCorrectGraph()
        {
            m_Store.Dispatch(new CreateGraphAssetAction(typeof(ClassStencil), "test", k_GraphPath));
            AssumeIntegrity();

            AssetDatabase.SaveAssets();
            Resources.UnloadAsset((Object)GraphModel.AssetModel);
            m_Store.Dispatch(new LoadGraphAssetAction(k_GraphPath));
            Assert.AreEqual(k_GraphPath, AssetDatabase.GetAssetPath((Object)GraphModel.AssetModel));
            AssertIntegrity();
        }

        [Test]
        public void CreateGraphActionBuildsValidGraphModel()
        {
            m_Store.Dispatch(new CreateGraphAssetAction(typeof(ClassStencil), "test", k_GraphPath));
            AssumeIntegrity();
        }

        [Test]
        public void CreateTestGraphBuildsValidGraphModel()
        {
            var graphTemplate = new TestGraph();
            m_Store.Dispatch(new CreateGraphAssetAction(typeof(ClassStencil), graphTemplate.DefaultAssetName, k_GraphPath, graphTemplate: graphTemplate));
            AssertIntegrity();
        }

        [Test]
        public void CreateTestGraphCanBeReloaded()
        {
            CreateTestGraphBuildsValidGraphModel();

            VSGraphModel graph = AssetDatabase.LoadAssetAtPath<VSGraphAssetModel>(k_GraphPath)?.GraphModel as VSGraphModel;
            Resources.UnloadAsset((Object)graph.AssetModel);
            m_Store.Dispatch(new LoadGraphAssetAction(k_GraphPath));

            AssertIntegrity();
        }

        [Test]
        public void CreateTestGraphFromAssetModel()
        {
            var graphTemplate = new TestGraph();
            var assetModel = ScriptableObject.CreateInstance<VSGraphAssetModel>();
            m_Store.Dispatch(new CreateGraphAssetFromModelAction(
                assetModel, graphTemplate, k_GraphPath, typeof(VSGraphModel)));
            AssertIntegrity();
        }
    }
}
