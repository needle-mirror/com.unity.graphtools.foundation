
using System;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    public class SearcherSizeTests
    {
        static readonly Vector2 k_DefaultSize = new Vector2(123, 456);
        const float k_DefaultRatio = 1.25f;
        static readonly Vector2 k_CreateNodeDefaultSize = new Vector2(789, 345);
        const float k_CreateNodeDefaultRatio = 2.05f;

        GraphToolState m_State;

        [SetUp]
        public void Setup()
        {
            var prefs = Preferences.CreatePreferences(nameof(SearcherSizeTests));
            m_State = new GraphToolState(new Hash128(), prefs);
            m_State.ResetSearcherSizes();
            m_State.SetInitialSearcherSize(null, k_DefaultSize, k_DefaultRatio);
            m_State.SetInitialSearcherSize(SearcherService.Usage.k_CreateNode, k_CreateNodeDefaultSize, k_CreateNodeDefaultRatio);
        }

        [Test]
        public void TestsSearcherSizeComputationAreOK()
        {
            var searcherSize = m_State.GetSearcherSize(SearcherService.Usage.k_CreateNode);
            Assert.AreEqual(k_CreateNodeDefaultSize, searcherSize.Size);
            Assert.AreEqual(k_CreateNodeDefaultRatio, searcherSize.RightLeftRatio);

            searcherSize = m_State.GetSearcherSize(null);
            Assert.AreEqual(k_DefaultSize, searcherSize.Size);
            Assert.AreEqual(k_DefaultRatio, searcherSize.RightLeftRatio);

            string someUsage = "some-usage";
            searcherSize = m_State.GetSearcherSize(someUsage);
            Assert.AreEqual(k_DefaultSize, searcherSize.Size);
            Assert.AreEqual(k_DefaultRatio, searcherSize.RightLeftRatio);

            Vector2 newSize = new Vector2(012.0f, 357.0f);
            float newRatio = 1.1f;

            m_State.SetSearcherSize(SearcherService.Usage.k_CreateNode, newSize, newRatio);
            searcherSize = m_State.GetSearcherSize(SearcherService.Usage.k_CreateNode);
            Assert.AreEqual(newSize, searcherSize.Size);
            Assert.AreEqual(newRatio, searcherSize.RightLeftRatio);

            m_State.SetSearcherSize(someUsage, newSize, newRatio);
            searcherSize = m_State.GetSearcherSize(someUsage);
            Assert.AreEqual(newSize, searcherSize.Size);
            Assert.AreEqual(newRatio, searcherSize.RightLeftRatio);

            searcherSize = m_State.GetSearcherSize(null);
            Assert.AreEqual(k_DefaultSize, searcherSize.Size);
            Assert.AreEqual(k_DefaultRatio, searcherSize.RightLeftRatio);
        }

    }
}
