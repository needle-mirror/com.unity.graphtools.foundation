using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Searcher;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public abstract class GraphElementSearcherAdapter : SearcherAdapter
    {
        protected VisualElement m_DetailsPanel;
        protected Label m_DetailsTitle;
        protected ScrollView m_Scrollview;

        protected GraphElementSearcherAdapter(string title) : base(title) {}

        public override void InitDetailsPanel(VisualElement detailsPanel)
        {
            m_DetailsPanel = detailsPanel;

            m_Scrollview = new ScrollView();
            m_Scrollview.StretchToParentSize();
            m_DetailsPanel.Add(m_Scrollview);

            m_DetailsTitle = new Label();
            m_DetailsPanel.Add(m_Scrollview);
        }

        public override void OnSelectionChanged(IEnumerable<SearcherItem> items)
        {
            if (m_DetailsPanel == null)
                return;

            var itemsList = items.ToList();
            if (!itemsList.Any())
                return;

            var searcherItem = itemsList.First();
            m_DetailsTitle.text = searcherItem.Name;

            var graphView = SearcherService.GraphView;
            foreach (var graphElement in graphView.GraphElements.ToList())
            {
                graphView.RemoveElement(graphElement);
            }

            if (!m_Scrollview.Contains(graphView))
            {
                m_Scrollview.Add(graphView);

                var eventCatcher = new VisualElement();
                eventCatcher.RegisterCallback<MouseDownEvent>(e => e.StopImmediatePropagation());
                eventCatcher.RegisterCallback<MouseMoveEvent>(e => e.StopImmediatePropagation());
                m_Scrollview.Add(eventCatcher);
                eventCatcher.StretchToParentSize();
            }

            var elements = CreateGraphElements(searcherItem).ToList();
            foreach (var element in elements.Where(element => element is INodeModel || element is IStickyNoteModel))
            {
                var node = GraphElementFactory.CreateUI<GraphElement>(graphView, graphView.Store, element);
                if (node != null)
                {
                    node.style.position = Position.Relative;
                    graphView.AddElement(node);
                }
            }

            OnGraphElementsCreated(searcherItem, elements);
        }

        protected virtual IEnumerable<IGraphElementModel> CreateGraphElements(SearcherItem item)
        {
            throw new NotImplementedException();
        }

        protected virtual void OnGraphElementsCreated(SearcherItem searcherItem,
            IEnumerable<IGraphElementModel> elements) {}
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
            return CreateGraphElementModels(m_GraphModel, item);
        }

        public static IEnumerable<IGraphElementModel> CreateGraphElementModels(IGraphModel mGraphModel, SearcherItem item)
        {
            return item is GraphNodeModelSearcherItem graphItem
                ? graphItem.CreateElements.Invoke(
                new GraphNodeCreationData(mGraphModel, Vector2.zero, SpawnFlags.Orphan))
                : Enumerable.Empty<IGraphElementModel>();
        }
    }
}
