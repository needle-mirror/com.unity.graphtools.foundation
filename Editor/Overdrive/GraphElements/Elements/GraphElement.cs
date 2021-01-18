using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public abstract class GraphElement : VisualElementBridge, ISelectableGraphElement, IGraphElement
    {
        static readonly CustomStyleProperty<int> s_LayerProperty = new CustomStyleProperty<int>("--layer");
        static readonly Color k_MinimapColor = new Color(0.9f, 0.9f, 0.9f, 0.5f);

        public static readonly string ussClassName = "ge-graph-element";
        public static readonly string selectableModifierUssClassName = ussClassName.WithUssModifier("selectable");

        int m_Layer;

        bool m_LayerIsInline;

        bool m_Selected;

        ClickSelector m_ClickSelector;

        ContextualMenuManipulator m_ContextualMenuManipulator;

        protected ContextualMenuManipulator ContextualMenuManipulator
        {
            get => m_ContextualMenuManipulator;
            set => this.ReplaceManipulator(ref m_ContextualMenuManipulator, value);
        }

        protected UIDependencies Dependencies { get; }

        public int Layer
        {
            get => m_Layer;
            set
            {
                m_LayerIsInline = true;
                m_Layer = value;
            }
        }

        public Color MinimapColor { get; protected set; }

        public virtual bool ShowInMiniMap { get; set; } = true;

        internal ResizeRestriction ResizeRestriction { get; set; }

        public bool Selected
        {
            get => m_Selected;
            set
            {
                // Set new value (toggle old value)
                if (!IsSelectable())
                    return;

                if (m_Selected == value)
                    return;

                m_Selected = value;

                this.SetCheckedPseudoState(m_Selected);
            }
        }

        public GraphElementPartList PartList { get; private set; }

        protected ClickSelector ClickSelector
        {
            get => m_ClickSelector;
            set => this.ReplaceManipulator(ref m_ClickSelector, value);
        }

        public IGraphElementModel Model { get; private set; }

        public Store Store { get; private set; }

        public GraphView GraphView { get; protected set; }

        public string Context { get; private set; }

        protected GraphElement()
        {
            MinimapColor = k_MinimapColor;
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);

            ContextualMenuManipulator = new ContextualMenuManipulator(BuildContextualMenu);

            Dependencies = new UIDependencies(this);
        }

        public void ResetLayer()
        {
            int prevLayer = m_Layer;
            m_Layer = 0;
            m_LayerIsInline = false;
            customStyle.TryGetValue(s_LayerProperty, out m_Layer);
            UpdateLayer(prevLayer);
        }

        protected virtual void OnCustomStyleResolved(ICustomStyle resolvedCustomStyle)
        {
            int prevLayer = m_Layer;
            if (!m_LayerIsInline)
                resolvedCustomStyle.TryGetValue(s_LayerProperty, out m_Layer);

            UpdateLayer(prevLayer);
        }

        void UpdateLayer(int prevLayer)
        {
            if (prevLayer != m_Layer)
                GraphView?.ChangeLayer(this);
        }

        protected virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
        }

        public void SetupBuildAndUpdate(IGraphElementModel model, Store store, GraphView graphView, string context = null)
        {
            Setup(model, store, graphView, context);
            BuildUI();
            UpdateFromModel();
        }

        public void Setup(IGraphElementModel model, Store store, GraphView graphView, string context)
        {
            Model = model;
            Store = store;
            GraphView = graphView;
            Context = context;

            // Used by graph view to restore selection on graph rebuild.
            viewDataKey = Model != null ? Model.Guid.ToString() : Guid.NewGuid().ToString();

            PartList = new GraphElementPartList();
            BuildPartList();
        }

        public void AddToGraphView(GraphView graphView)
        {
            GraphView = graphView;
            UIForModel.AddOrReplaceGraphElement(this);
        }

        public void RemoveFromGraphView()
        {
            Dependencies.ClearDependencyLists();
            UIForModel.RemoveGraphElement(this);
            GraphView = null;
        }

        protected virtual void BuildPartList() {}

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

        protected virtual void ClearElementUI()
        {
            Clear();
        }

        protected virtual void BuildElementUI()
        {
            AddToClassList(ussClassName);
        }

        protected virtual void PostBuildUI() {}

        public void UpdateFromModel()
        {
            if (Store?.State?.Preferences.GetBool(BoolPref.LogUIUpdate) ?? false)
            {
                Debug.LogWarning($"Rebuilding {this}");
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

        protected virtual void UpdateElementFromModel()
        {
            ClickSelector = IsSelectable() ? new ClickSelector() : null;

            EnableInClassList(selectableModifierUssClassName, IsSelectable() && ClickSelector != null);
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

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            Dependencies.OnGeometryChanged(evt);
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            OnCustomStyleResolved(evt.customStyle);
            Dependencies.OnCustomStyleResolved(evt);
        }

        void OnDetachedFromPanel(DetachFromPanelEvent evt)
        {
            Dependencies.OnDetachedFromPanel(evt);
        }

        public virtual bool IsSelectable()
        {
            return Model?.IsSelectable() ?? false;
        }

        public virtual bool IsMovable()
        {
            return Model?.IsMovable() ?? false;
        }

        public virtual bool IsDeletable()
        {
            return Model?.IsDeletable() ?? false;
        }

        public virtual bool IsResizable()
        {
            return Model?.IsResizable() ?? false;
        }

        public virtual bool IsDroppable()
        {
            return Model?.IsDroppable() ?? false;
        }

        public virtual bool IsRenamable()
        {
            return Model?.IsRenamable() ?? false;
        }

        public virtual bool IsCopiable()
        {
            return Model?.IsCopiable() ?? false;
        }

        // PF: remove
        public Rect GetPosition()
        {
            return layout;
        }

        public virtual void SetPosition(Rect newPos)
        {
            style.left = newPos.x;
            style.top = newPos.y;
        }

        public virtual void OnSelected()
        {
        }

        public virtual void OnUnselected()
        {
        }

        public virtual void Select(VisualElement selectionContainer, bool additive)
        {
            if (selectionContainer is ISelection selection)
            {
                if (!selection.Selection.Contains(this))
                {
                    if (!additive)
                        selection.ClearSelection();

                    selection.AddToSelection(this);
                }
            }
        }

        public virtual void Unselect(VisualElement  selectionContainer)
        {
            if (selectionContainer is ISelection selection)
            {
                if (selection.Selection.Contains(this))
                {
                    selection.RemoveFromSelection(this);
                }
            }
        }

        public virtual bool IsSelected(VisualElement selectionContainer)
        {
            if (selectionContainer is ISelection selection)
            {
                if (selection.Selection.Contains(this))
                {
                    return true;
                }
            }

            return false;
        }

        public virtual void Rename()
        {
            var editableLabel = this.Q<EditableLabel>();
            editableLabel.BeginEditing();
        }
    }
}
