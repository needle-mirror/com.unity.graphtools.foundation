using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public abstract class ModelUI : VisualElement, IModelUI
    {
        public IGraphElementModel Model { get; private set; }

        public CommandDispatcher CommandDispatcher { get; private set; }

        public GraphView GraphView { get; protected set; }

        public string Context { get; private set; }

        public ModelUIPartList PartList { get; private set; }

        protected UIDependencies Dependencies { get; }

        ContextualMenuManipulator m_ContextualMenuManipulator;

        protected ModelUI()
        {
            Dependencies = new UIDependencies(this);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);

            ContextualMenuManipulator = new ContextualMenuManipulator(BuildContextualMenu);
        }

        protected ContextualMenuManipulator ContextualMenuManipulator
        {
            get => m_ContextualMenuManipulator;
            set => this.ReplaceManipulator(ref m_ContextualMenuManipulator, value);
        }

        /// <summary>
        /// Builds the list of parts for this UI Element.
        /// </summary>
        protected virtual void BuildPartList() { }

        public void SetupBuildAndUpdate(IGraphElementModel model, CommandDispatcher commandDispatcher, GraphView graphView, string context = null)
        {
            Setup(model, commandDispatcher, graphView, context);
            BuildUI();
            UpdateFromModel();
        }

        public void Setup(IGraphElementModel model, CommandDispatcher commandDispatcher, GraphView graphView, string context)
        {
            Model = model;
            CommandDispatcher = commandDispatcher;
            GraphView = graphView;
            Context = context;

            PartList = new ModelUIPartList();
            BuildPartList();
        }

        public void BuildUI()
        {
            ClearElementUI();
            BuildElementUI();

            foreach (var component in PartList)
            {
                component.BuildUI(this);
            }

            foreach (var component in PartList)
            {
                component.PostBuildUI();
            }

            PostBuildUI();
        }

        public void UpdateFromModel()
        {
            if (CommandDispatcher?.GraphToolState?.Preferences.GetBool(BoolPref.LogUIUpdate) ?? false)
            {
                Debug.Log($"Rebuilding {this}");
                if (GraphView == null)
                {
                    Debug.LogWarning($"Updating a graph element that is not attached to a graph view: {this}");
                }
            }

            UpdateElementFromModel();

            foreach (var component in PartList)
            {
                component.UpdateFromModel();
            }

            Dependencies.UpdateDependencyLists();
        }

        protected virtual void ClearElementUI()
        {
            Clear();
        }

        protected virtual void BuildElementUI()
        {
        }

        protected virtual void PostBuildUI()
        {
        }

        /// <summary>
        /// Update the element to reflect the state of the attached model.
        /// </summary>
        protected virtual void UpdateElementFromModel()
        {
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            Dependencies.OnCustomStyleResolved(evt);
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            Dependencies.OnGeometryChanged(evt);
        }

        void OnDetachedFromPanel(DetachFromPanelEvent evt)
        {
            Dependencies.OnDetachedFromPanel(evt);
        }

        public virtual void AddForwardDependencies()
        {
        }

        public virtual void AddBackwardDependencies()
        {
        }

        public virtual void AddModelDependencies()
        {
        }

        protected virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
        }

        public virtual void AddToGraphView(GraphView graphView)
        {
            GraphView = graphView;
            UIForModel.AddOrReplaceGraphElement(this);

            if (PartList != null)
            {
                foreach (var component in PartList)
                {
                    component.OwnerAddedToView();
                }
            }
        }

        public virtual void RemoveFromGraphView()
        {
            if (PartList != null)
            {
                foreach (var component in PartList)
                {
                    component.OwnerRemovedFromView();
                }
            }

            Dependencies.ClearDependencyLists();
            UIForModel.RemoveGraphElement(this);
            GraphView = null;
        }
    }
}
