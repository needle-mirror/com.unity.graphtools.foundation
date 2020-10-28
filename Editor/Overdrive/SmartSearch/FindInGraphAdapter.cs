using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Searcher;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class FindInGraphAdapter : SimpleSearcherAdapter
    {
        readonly Action<FindSearcherItem> m_OnHighlightDelegate;

        public class FindSearcherItem : SearcherItem
        {
            public FindSearcherItem(INodeModel node, string title, string help = "", List<SearcherItem> children = null)
                : base(title, help, children)
            {
                Node = node;
            }

            public INodeModel Node { get; }
        }

        public FindInGraphAdapter(Action<FindSearcherItem> onHighlightDelegate) : base("Find in graph")
        {
            m_OnHighlightDelegate = onHighlightDelegate;
        }

        public override void OnSelectionChanged(IEnumerable<SearcherItem> items)
        {
            var selectedItems = items.ToList();

            if (selectedItems.Count > 0 && selectedItems[0] is FindSearcherItem fsi)
                m_OnHighlightDelegate(fsi);

            base.OnSelectionChanged(selectedItems);
        }
    }
}
