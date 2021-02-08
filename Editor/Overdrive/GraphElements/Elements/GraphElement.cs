using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public abstract class GraphElement : ModelUI, ISelectableGraphElement
    {
        static readonly CustomStyleProperty<int> s_LayerProperty = new CustomStyleProperty<int>("--layer");
        static readonly Color k_MinimapColor = new Color(0.9f, 0.9f, 0.9f, 0.5f);

        public static readonly string ussClassName = "ge-graph-element";
        public static readonly string selectableModifierUssClassName = ussClassName.WithUssModifier("selectable");

        int m_Layer;

        bool m_LayerIsInline;

        bool m_Selected;

        ClickSelector m_ClickSelector;

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

        protected ClickSelector ClickSelector
        {
            get => m_ClickSelector;
            set => this.ReplaceManipulator(ref m_ClickSelector, value);
        }

        protected GraphElement()
        {
            MinimapColor = k_MinimapColor;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        public void ResetLayer()
        {
            int prevLayer = m_Layer;
            m_Layer = 0;
            m_LayerIsInline = false;
            customStyle.TryGetValue(s_LayerProperty, out m_Layer);
            UpdateLayer(prevLayer);
        }

        void UpdateLayer(int prevLayer)
        {
            if (prevLayer != m_Layer)
                GraphView?.ChangeLayer(this);
        }

        protected override void PostBuildUI()
        {
            AddToClassList(ussClassName);
        }

        protected override void UpdateElementFromModel()
        {
            ClickSelector = IsSelectable() ? new ClickSelector() : null;

            EnableInClassList(selectableModifierUssClassName, IsSelectable() && ClickSelector != null);
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            int prevLayer = m_Layer;
            if (!m_LayerIsInline)
                evt.customStyle.TryGetValue(s_LayerProperty, out m_Layer);

            UpdateLayer(prevLayer);
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
