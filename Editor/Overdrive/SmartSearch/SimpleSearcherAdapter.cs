using System;
using UnityEditor.Searcher;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class SimpleSearcherAdapter : SearcherAdapter
    {
        public SimpleSearcherAdapter(string title)
            : base(title) { }

        // TODO: Disable details panel for now
        public override bool HasDetailsPanel => false;
    }
}
