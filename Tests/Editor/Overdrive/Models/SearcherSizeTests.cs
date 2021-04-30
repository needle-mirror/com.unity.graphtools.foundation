
using System;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    public class SearcherSizeTests
    {
        static readonly Vector2 k_DefaultSize = new Vector2(123, 456);
        private const float k_DefaultRatio = 1.25f;
        static readonly Vector2 k_CreateNodeDefaultSize = new Vector2(789, 345);
        private const float k_CreateNodeDefaultRatio = 2.05f;

        class TestStencil : Stencil
        {
            public TestStencil()
            {
                SetSearcherSize(null, k_DefaultSize, k_DefaultRatio);
                SetSearcherSize(SearcherService.Usage.k_CreateNode, k_CreateNodeDefaultSize, k_CreateNodeDefaultRatio);
            }

            /// <inheritdoc />
            public override IBlackboardGraphModel CreateBlackboardGraphModel(IGraphAssetModel graphAssetModel)
            {
                throw new NotImplementedException();
            }

            public override string ToolName => "TestTool";
        }

        private TestStencil m_Stencil;

        [SetUp]
        public void Setup()
        {
            m_Stencil = new TestStencil();
        }

        [Test]
        public void TestsSearcherSizeComputationAreOK()
        {
            Vector2 position = new Vector2(147, 258);

            Rect rect = m_Stencil.GetSearcherRect(position, out float rightLeftRatio, SearcherService.Usage.k_CreateNode);
            Assert.AreEqual(new Rect(position, k_CreateNodeDefaultSize), rect);
            Assert.AreEqual(k_CreateNodeDefaultRatio, rightLeftRatio);

            rect = m_Stencil.GetSearcherRect(position, out rightLeftRatio, null);
            Assert.AreEqual(new Rect(position, k_DefaultSize), rect);
            Assert.AreEqual(k_DefaultRatio, rightLeftRatio);

            string someUsage = "some-usage";
            rect = m_Stencil.GetSearcherRect(position, out rightLeftRatio, someUsage);
            Assert.AreEqual(new Rect(position, k_DefaultSize), rect);
            Assert.AreEqual(k_DefaultRatio, rightLeftRatio);

            Vector2 newSize = new Vector2(012.0f, 357.0f);
            float newRatio = 1.1f;

            m_Stencil.SetSearcherSize(SearcherService.Usage.k_CreateNode, newSize, newRatio);
            rect = m_Stencil.GetSearcherRect(position, out rightLeftRatio, SearcherService.Usage.k_CreateNode);
            Assert.AreEqual(new Rect(position, newSize), rect);
            Assert.AreEqual(newRatio, rightLeftRatio);

            m_Stencil.SetSearcherSize(someUsage, newSize, newRatio);
            rect = m_Stencil.GetSearcherRect(position, out rightLeftRatio, someUsage);
            Assert.AreEqual(new Rect(position, newSize), rect);
            Assert.AreEqual(newRatio, rightLeftRatio);

            rect = m_Stencil.GetSearcherRect(position, out rightLeftRatio, null);
            Assert.AreEqual(new Rect(position, k_DefaultSize), rect);
            Assert.AreEqual(k_DefaultRatio, rightLeftRatio);


        }

    }
}
