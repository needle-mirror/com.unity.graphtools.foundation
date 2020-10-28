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

        public static readonly string k_UssClassName = "ge-graph-element";
        public static readonly string k_SelectableModifierUssClassName = k_UssClassName.WithUssModifier("selectable");

        int m_Layer;

        bool m_LayerIsInline;

        bool m_Selected;

        protected ContextualMenuManipulator m_ContextualMenuManipulator;

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

        protected ClickSelector ClickSelector { get; private set; }

        public IGraphElementModel Model { get; private set; }

        // PF make setter private (needed by Blackboard)
        public Store Store { get; protected set; }

        // PF make setter private (needed by Blackboard)
        public GraphView GraphView { get; protected set; }

        protected GraphElement()
        {
            MinimapColor = k_MinimapColor;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);

            m_ContextualMenuManipulator = new ContextualMenuManipulator(BuildContextualMenu);
            this.AddManipulator(m_ContextualMenuManipulator);
        }

        public void ResetLayer()
        {
            int prevLayer = m_Layer;
            m_Layer = 0;
            m_LayerIsInline = false;
            customStyle.TryGetValue(s_LayerProperty, out m_Layer);
            UpdateLayer(prevLayer);
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            OnCustomStyleResolved(e.customStyle);
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
            {
                GraphView view = GetFirstAncestorOfType<GraphView>();
                if (view != null)
                {
                    view.ChangeLayer(this);
                }
            }
        }

        protected virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
        }

        public void SetupBuildAndUpdate(IGraphElementModel model, Store store, GraphView graphView)
        {
            Setup(model, store, graphView);
            BuildUI();
            UpdateFromModel();
        }

        public void Setup(IGraphElementModel model, Store store, GraphView graphView)
        {
            Model = model;
            Store = store;
            GraphView = graphView;

            // Used by graph view to restore selection on graph rebuild.
            viewDataKey = Model != null ? Model.Guid.ToString() : Guid.NewGuid().ToString();

            PartList = new GraphElementPartList();
            BuildPartList();
        }

        protected virtual void BuildPartList() {}

        public void BuildUI()
        {
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

        protected virtual void BuildElementUI()
        {
            AddToClassList(k_UssClassName);

            // PF: Uncomment when all graph elements have a clean Setup/BuildUI/UpdateFromModel path.
            // BlackboardVariableField do not.
            // FIXME: make it a separate step from BuildUI(), since subclasses may want to do stuff before their base.
            // Clear();
        }

        protected virtual void PostBuildUI() {}

        public void UpdateFromModel()
        {
            UpdateElementFromModel();

            foreach (var component in PartList)
            {
                component.UpdateFromModel();
            }
        }

        protected virtual void UpdateElementFromModel()
        {
            if (IsSelectable() && ClickSelector == null)
            {
                ClickSelector = new ClickSelector();
                this.AddManipulator(ClickSelector);
            }
            else if (!IsSelectable() && ClickSelector != null)
            {
                this.RemoveManipulator(ClickSelector);
                ClickSelector = null;
            }

            EnableInClassList(k_SelectableModifierUssClassName, IsSelectable() && ClickSelector != null);
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
            var selection = selectionContainer as ISelection;
            if (selection != null)
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
            var selection = selectionContainer as ISelection;
            if (selection != null)
            {
                if (selection.Selection.Contains(this))
                {
                    selection.RemoveFromSelection(this);
                }
            }
        }

        public virtual bool IsSelected(VisualElement selectionContainer)
        {
            var selection = selectionContainer as ISelection;
            if (selection != null)
            {
                if (selection.Selection.Contains(this))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
