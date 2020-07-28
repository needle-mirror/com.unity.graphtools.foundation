using System;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Graph
{
    class TestGraph : ICreatableGraphTemplate
    {
        public Type StencilType => typeof(ClassStencil);
        public string GraphTypeName => "Test Graph";
        public string DefaultAssetName => "testgraph";

        public void InitBasicGraph(IGTFGraphModel graph)
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
            m_Store.Dispatch(new CreateGraphAssetAction(typeof(ClassStencil), typeof(TestGraphAssetModel), "test", k_GraphPath));
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
            m_Store.Dispatch(new CreateGraphAssetAction(typeof(ClassStencil), typeof(TestGraphAssetModel), "test", k_GraphPath));
            AssumeIntegrity();
        }

        [Test]
        public void CreateTestGraphBuildsValidGraphModel()
        {
            var graphTemplate = new TestGraph();
            m_Store.Dispatch(new CreateGraphAssetAction(typeof(ClassStencil), typeof(TestGraphAssetModel), graphTemplate.DefaultAssetName, k_GraphPath, graphTemplate: graphTemplate));
            AssertIntegrity();
        }

        [Test]
        public void CreateTestGraphCanBeReloaded()
        {
            CreateTestGraphBuildsValidGraphModel();

            GraphModel graph = AssetDatabase.LoadAssetAtPath<GraphAssetModel>(k_GraphPath)?.GraphModel as GraphModel;
            Resources.UnloadAsset((Object)graph?.AssetModel);
            m_Store.Dispatch(new LoadGraphAssetAction(k_GraphPath));

            AssertIntegrity();
        }

        [Test]
        public void CreateTestGraphFromAssetModel()
        {
            var graphTemplate = new TestGraph();
            var assetModel = ScriptableObject.CreateInstance<TestGraphAssetModel>();
            m_Store.Dispatch(new CreateGraphAssetFromModelAction(
                assetModel, graphTemplate, k_GraphPath));
            AssertIntegrity();
        }
    }
}
