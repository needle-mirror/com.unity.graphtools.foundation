using System;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.Helpers;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class StickyNoteUICreationTests
    {
        GraphView m_GraphView;
        Store m_Store;
        GraphModel m_GraphModel;

        [SetUp]
        public void SetUp()
        {
            m_GraphModel = new GraphModel();
            m_Store = new Store(new Helpers.TestState(m_GraphModel), StoreHelper.RegisterReducers);
            m_GraphView = new TestGraphView(m_Store);
        }

        [Test]
        public void StickyNoteHasExpectedParts()
        {
            var stickyNoteModel = new StickyNoteModel();
            var stickyNote = new StickyNote();
            stickyNote.SetupBuildAndUpdate(stickyNoteModel, m_Store, m_GraphView);

            Assert.IsNotNull(stickyNote.Q<VisualElement>(StickyNote.k_TitleContainerPartName));
            Assert.IsNotNull(stickyNote.Q<VisualElement>(StickyNote.k_ContentContainerPartName));
            Assert.IsNotNull(stickyNote.Q<VisualElement>(StickyNote.k_ResizerPartName));
        }
    }
}
