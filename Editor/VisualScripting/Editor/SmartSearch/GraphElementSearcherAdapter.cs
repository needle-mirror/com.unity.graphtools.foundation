using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Searcher;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor.SmartSearch
{
    public abstract class GraphElementSearcherAdapter : SearcherAdapter
    {
        protected VisualElement m_DetailsPanel;
        protected Label m_DetailsTitle;

        protected GraphElementSearcherAdapter(string title) : base(title) {}

        public override void InitDetailsPanel(VisualElement detailsPanel)
        {
            m_DetailsPanel = detailsPanel;
            m_DetailsTitle = new Label();
            m_DetailsPanel.Add(m_DetailsTitle);
        }

        public override void OnSelectionChanged(IEnumerable<SearcherItem> items)
        {
            if (m_DetailsPanel == null)
                return;

            var itemsList = items.ToList();
            m_DetailsTitle.text = itemsList.First().Name;

            var graphView = SearcherService.GraphView;
            foreach (var graphElement in graphView.graphElements.ToList())
            {
                graphView.RemoveElement(graphElement);
            }

            if (!m_DetailsPanel.Contains(graphView))
            {
                m_DetailsPanel.Add(graphView);

                var eventCatcher = new VisualElement();
                eventCatcher.RegisterCallback<MouseDownEvent>(e => e.StopImmediatePropagation());
                eventCatcher.RegisterCallback<MouseMoveEvent>(e => e.StopImmediatePropagation());
                m_DetailsPanel.Add(eventCatcher);
                eventCatcher.StretchToParentSize();
            }

            var elements = CreateGraphElements(itemsList.First());
            foreach (var element in elements)
            {
                if (element is INodeModel || element is IStickyNoteModel)
                    graphView.AddElement(GraphElementFactory.CreateUI(graphView, graphView.store, element));
            }
        }

        protected virtual IEnumerable<IGraphElementModel> CreateGraphElements(SearcherItem item)
        {
            throw new NotImplementedException();
        }
    }

    public class GraphNodeSearcherAdapter : GraphElementSearcherAdapter
    {
        readonly IGraphModel m_GraphModel;

        public GraphNodeSearcherAdapter(IGraphModel graphModel, string title)
            : base(title)
        {
            m_GraphModel = graphModel;
        }

        protected override IEnumerable<IGraphElementModel> CreateGraphElements(SearcherItem item)
        {
            return item is GraphNodeModelSearcherItem graphItem
                ? graphItem.CreateElements.Invoke(
                new GraphNodeCreationData(m_GraphModel, Vector2.zero, SpawnFlags.Orphan))
                : Enumerable.Empty<IGraphElementModel>();
        }
    }

    public class StackNodeSearcherAdapter : GraphElementSearcherAdapter
    {
        readonly IStackModel m_StackModel;

        public StackNodeSearcherAdapter(IStackModel stackModel, string title)
            : base(title)
        {
            m_StackModel = stackModel;
        }

        protected override IEnumerable<IGraphElementModel> CreateGraphElements(SearcherItem item)
        {
            return item is StackNodeModelSearcherItem stackItem
                ? stackItem.CreateElements.Invoke(new StackNodeCreationData(m_StackModel, -1, spawnFlags: SpawnFlags.Orphan))
                : Enumerable.Empty<IGraphElementModel>();
        }
    }
}
