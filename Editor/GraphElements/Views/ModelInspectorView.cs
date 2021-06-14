using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A view to display the model inspector.
    /// </summary>
    public class ModelInspectorView : VisualElement, IModelView
    {
        class ModelInspectorObserver : StateObserver<GraphToolState>
        {
            ModelInspectorView m_Inspector;

            public ModelInspectorObserver(ModelInspectorView inspector)
                : base(nameof(GraphToolState.ModelInspectorState), nameof(GraphToolState.GraphViewState))
            {
                m_Inspector = inspector;
            }

            protected override void Observe(GraphToolState state)
            {
                if (m_Inspector?.panel != null)
                    m_Inspector.Update(this, state);
            }
        }

        public static readonly string ussClassName = "model-inspector";
        public static readonly string titleUssClassName = ussClassName.WithUssElement("title");
        public static readonly string containerUssClassName = ussClassName.WithUssElement("container");

        ModelInspectorObserver m_Observer;
        CommandDispatcher m_Dispatcher;

        Label m_SidePanelTitle;
        VisualElement m_SidePanelInspectorContainer;
        ModelUI m_Inspector;

        /// <inheritdoc />
        public CommandDispatcher CommandDispatcher => m_Dispatcher;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelInspectorView"/> class.
        /// </summary>
        /// <param name="commandDispatcher">The dispatcher to use to dispatch commands when the model is modified.</param>
        public ModelInspectorView(CommandDispatcher commandDispatcher)
        {
            m_Dispatcher = commandDispatcher;
            AddToClassList(ussClassName);

            m_SidePanelTitle = new Label();
            m_SidePanelTitle.AddToClassList(titleUssClassName);
            Add(m_SidePanelTitle);

            m_SidePanelInspectorContainer = new VisualElement();
            m_SidePanelInspectorContainer.AddToClassList(containerUssClassName);
            Add(m_SidePanelInspectorContainer);

            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);

            this.AddStylesheet("ModelInspector.uss");
        }

        /// <summary>
        /// Callback for the <see cref="AttachToPanelEvent"/>.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnEnterPanel(AttachToPanelEvent e)
        {
            m_Observer ??= new ModelInspectorObserver(this);
            m_Dispatcher?.RegisterObserver(m_Observer);
        }

        /// <summary>
        /// Callback for the <see cref="DetachFromPanelEvent"/>.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnLeavePanel(DetachFromPanelEvent e)
        {
            m_Dispatcher?.UnregisterObserver(m_Observer);
        }

        void Update(IStateObserver observer, GraphToolState state)
        {
            using (var observation = observer.ObserveState(state.ModelInspectorState))
            {
                var rebuildType = observation.UpdateType;

                if (rebuildType == UpdateType.Complete)
                {
                    m_SidePanelInspectorContainer.Clear();
                    if (state.ModelInspectorState.EditedNode != null)
                    {
                        m_SidePanelTitle.text = (state.ModelInspectorState.EditedNode as IHasTitle)?.Title ?? "Node Inspector";
                        m_Inspector = GraphElementFactory.CreateUI<ModelUI>(this, CommandDispatcher, state.ModelInspectorState.EditedNode);
                        if (m_Inspector != null)
                            m_SidePanelInspectorContainer.Add(m_Inspector);
                    }
                    else
                    {
                        m_Inspector = null;
                        m_SidePanelTitle.text = "Node Inspector";
                    }
                }
            }

            using (var gvObservation = observer.ObserveState(state.GraphViewState))
            {
                var rebuildType = gvObservation.UpdateType;

                if (rebuildType != UpdateType.None)
                {
                    m_SidePanelTitle.text = (state.ModelInspectorState.EditedNode as IHasTitle)?.Title ?? "Node Inspector";
                    m_Inspector?.UpdateFromModel();
                }
            }
        }
    }
}
