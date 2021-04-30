using System;
using UnityEditor.Searcher;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class SimpleSearcherAdapter : SearcherAdapter, IGTFSearcherAdapter
    {
        public SimpleSearcherAdapter(string title)
            : base(title)
        {
        }

        // TODO: Disable details panel for now
        public override bool HasDetailsPanel => false;

        float m_InitialSplitterDetailRatio = 1.0f;
        public override float InitialSplitterDetailRatio
        {
            get => m_InitialSplitterDetailRatio;
        }

        public void SetInitialSplitterDetailRatio(float ratio)
        {
            m_InitialSplitterDetailRatio = ratio;
        }
    }
}
