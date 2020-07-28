using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class Blackboard : GraphElement, ISelection
    {
        protected const string k_ClassLibraryTitle = "Blackboard";

        public delegate void RebuildCallback(RebuildMode rebuildMode);

        public enum RebuildMode
        {
            BlackboardOnly,
            BlackboardAndGraphView
        }

        VisualElement m_MainContainer;
        VisualElement m_Root;
        Label m_TitleLabel;
        Label m_SubTitleLabel;
        ScrollView m_ScrollView;
        VisualElement m_ContentContainer;
        VisualElement m_HeaderItem;
        Button m_AddButton;
        bool m_Scrollable = true;

        Dragger m_Dragger;

        internal static readonly string StyleSheetPath = "Blackboard.uss";

        public Action<Blackboard> addItemRequested { get; set; }
        public Action<Blackboard, int, VisualElement> moveItemRequested { get; set; }
        public Action<Blackboard, VisualElement, string> editTextRequested { get; set; }

        // ISelection implementation
        public List<ISelectableGraphElement> Selection
        {
            get
            {
                return GraphView?.Selection;
            }
        }

        protected string title
        {
            set => m_TitleLabel.text = value;
        }

        protected string subTitle
        {
            set => m_SubTitleLabel.text = value;
        }

        bool m_Windowed;
        public bool windowed
        {
            set
            {
                if (m_Windowed == value) return;

                if (value)
                {
                    AddToClassList("windowed");
                    this.RemoveManipulator(m_Dragger);
                }
                else
                {
                    RemoveFromClassList("windowed");
                    this.AddManipulator(m_Dragger);
                }
                m_Windowed = value;
            }
        }

        // PF: remove Is..
        public override bool IsPositioned()
        {
            return !m_Windowed;
        }

        public override bool IsResizable()
        {
            return true;
        }

        public override VisualElement contentContainer { get { return m_ContentContainer; } }

        public bool scrollable
        {
            get
            {
                return m_Scrollable;
            }
            set
            {
                if (m_Scrollable == value)
                    return;

                m_Scrollable = value;

                if (m_Scrollable)
                {
                    if (m_ScrollView == null)
                    {
                        m_ScrollView = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
                    }

                    // Remove the sections container from the content item and add it to the scrollview
                    m_ContentContainer.RemoveFromHierarchy();
                    m_Root.Add(m_ScrollView);
                    m_ScrollView.Add(m_ContentContainer);
                    ResizeRestriction = ResizeRestriction.None; // As both the width and height can be changed by the user using a resizer

                    AddToClassList("scrollable");
                }
                else
                {
                    if (m_ScrollView != null)
                    {
                        // Remove the sections container from the scrollview and add it to the content item
                        ResizeRestriction = ResizeRestriction.FlexDirection; // As the height is automatically computed from the content but the width can be changed by the user using a resizer
                        m_ScrollView.RemoveFromHierarchy();
                        m_ContentContainer.RemoveFromHierarchy();
                        m_Root.Add(m_ContentContainer);
                    }
                    RemoveFromClassList("scrollable");
                }
            }
        }

        public Blackboard(Store store, GraphView associatedGraphView)
        {
            Store = store;
            GraphView = associatedGraphView;

            var tpl = GraphElementHelper.LoadUXML("Blackboard.uxml");
            this.AddStylesheet(StyleSheetPath);

            m_MainContainer = tpl.Instantiate();
            m_MainContainer.AddToClassList("mainContainer");

            m_Root = m_MainContainer.Q("content");

            m_HeaderItem = m_MainContainer.Q("header");
            m_HeaderItem.AddToClassList("blackboardHeader");

            m_AddButton = m_MainContainer.Q(name: "addButton") as Button;
            m_AddButton.clickable.clicked += () =>
            {
                addItemRequested?.Invoke(this);
            };

            m_TitleLabel = m_MainContainer.Q<Label>(name: "titleLabel");
            m_SubTitleLabel = m_MainContainer.Q<Label>(name: "subTitleLabel");
            m_ContentContainer = m_MainContainer.Q(name: "contentContainer");

            hierarchy.Add(m_MainContainer);

            style.overflow = Overflow.Hidden;

            ClearClassList();
            AddToClassList("blackboard");

            m_Dragger = new Dragger { clampToParentEdges = true };
            this.AddManipulator(m_Dragger);

            scrollable = false;

            hierarchy.Add(new Resizer());

            RegisterCallback<DragUpdatedEvent>(e =>
            {
                e.StopPropagation();
            });

            // event interception to prevent GraphView manipulators from being triggered
            // when working with the blackboard

            // prevent Zoomer manipulator
            RegisterCallback<WheelEvent>(e =>
            {
                e.StopPropagation();
            });

            RegisterCallback<MouseDownEvent>(e =>
            {
                if (e.button == (int)MouseButton.LeftMouse)
                    ClearSelection();
                // prevent ContentDragger manipulator
                e.StopPropagation();
            });

            RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
            RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            focusable = true;
        }

        public virtual void AddToSelection(ISelectableGraphElement selectable)
        {
            GraphView?.AddToSelection(selectable);
        }

        public virtual void RemoveFromSelection(ISelectableGraphElement selectable)
        {
            GraphView?.RemoveFromSelection(selectable);
        }

        public virtual void ClearSelection()
        {
            GraphView?.ClearSelection();
        }

        public void RestoreSelectionForElement(GraphElement element)
        {
            var editorDataModel = Store.GetState().EditorDataModel;

            // Select upon creation...
            if (element is IGraphElement hasElementGraphModel &&
                (editorDataModel?.ShouldSelectElementUponCreation(hasElementGraphModel.Model) ?? false))
                element.Select(GraphView, true);

            // ...or regular selection
            // TODO: This bypasses the problems with selection in GraphView when selection contains
            // non layered elements (like Blackboard fields for example)
            {
                if (!GraphView.PersistentSelectionContainsElement(element) ||
                    GraphView.Selection.Contains(element) && element.Selected)
                    return;

                element.Selected = true;
                if (!GraphView.Selection.Contains(element))
                    Selection.Add(element);
                element.OnSelected();

                // To ensure that the selected GraphElement gets unselected if it is removed from the GraphView.
                element.RegisterCallback<DetachFromPanelEvent>(OnSelectedElementDetachedFromPanel);

                element.MarkDirtyRepaint();
            }
        }

        void OnSelectedElementDetachedFromPanel(DetachFromPanelEvent evt)
        {
            var selectable = evt.target as ISelectableGraphElement;
            if (!(selectable is GraphElement graphElement))
                return;

            graphElement.Selected = false;
            Selection.Remove(selectable);
            graphElement.OnUnselected();
            graphElement.UnregisterCallback<DetachFromPanelEvent>(OnSelectedElementDetachedFromPanel);
            graphElement.MarkDirtyRepaint();
        }

        void OnValidateCommand(ValidateCommandEvent evt)
        {
            GraphView?.OnValidateCommand(evt);
        }

        void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            GraphView?.OnExecuteCommand(evt);
        }

        public List<BlackboardSection> Sections { get; private set; }
        public List<IHighlightable> GraphVariables { get; } = new List<IHighlightable>();

        public void ClearContents()
        {
            if (Sections != null)
            {
                foreach (var section in Sections)
                {
                    section.Clear();
                }
            }

            GraphVariables.Clear();

            IGTFGraphModel currentGraphModel = null;
            if (!(Store.GetState().AssetModel as ScriptableObject))
            {
                title = k_ClassLibraryTitle;
                subTitle = "";
            }
            else
            {
                currentGraphModel = Store.GetState().CurrentGraphModel;
                title = currentGraphModel.FriendlyScriptName;
                subTitle = currentGraphModel.Stencil?.GetBlackboardProvider().GetSubTitle();
            }

            var blackboardProvider = currentGraphModel?.Stencil?.GetBlackboardProvider();
            if (m_AddButton != null)
                if (blackboardProvider == null || blackboardProvider.CanAddItems == false)
                    m_AddButton.style.visibility = Visibility.Hidden;
        }

        IBlackboardProvider m_LastProvider;

        public void Rebuild(RebuildMode rebuildMode)
        {
            IBlackboardProvider blackboardProvider = Store.GetState().CurrentGraphModel.Stencil?.GetBlackboardProvider();
            if (Sections == null || m_LastProvider != blackboardProvider)
            {
                m_LastProvider = blackboardProvider;
                ClearContents();
                Clear();
                Sections = blackboardProvider?.CreateSections().ToList();
                Sections?.ForEach(Add);
            }

            if (rebuildMode == RebuildMode.BlackboardAndGraphView)
                Store.ForceRefreshUI(UpdateFlags.GraphTopology);
            else
                RebuildBlackboard();
        }

        protected virtual void RebuildBlackboard()
        {
            var currentGraphModel = Store.GetState().CurrentGraphModel;
            title = currentGraphModel.FriendlyScriptName;

            subTitle = currentGraphModel.Stencil?.GetBlackboardProvider()?.GetSubTitle();

            var blackboardProvider = currentGraphModel.Stencil?.GetBlackboardProvider();
            if (m_AddButton != null)
                if (blackboardProvider == null || !blackboardProvider.CanAddItems)
                    m_AddButton.style.visibility = Visibility.Hidden;
                else
                    m_AddButton.style.visibility = StyleKeyword.Null;

            RebuildSections();

            GraphView.HighlightGraphElements();
        }

        protected void RebuildSections()
        {
            if (Sections != null)
            {
                var currentGraphModel = Store.GetState().CurrentGraphModel;
                var blackboardProvider = currentGraphModel.Stencil.GetBlackboardProvider();
                blackboardProvider.RebuildSections(this);
            }
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            RegisterCallback<KeyDownEvent>(DisplayAppropriateSearcher);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            UnregisterCallback<KeyDownEvent>(DisplayAppropriateSearcher);
        }

        void DisplayAppropriateSearcher(KeyDownEvent e)
        {
            if (e.keyCode == KeyCode.Space)
                m_LastProvider.DisplayAppropriateSearcher(e.originalMousePosition, this);
        }
    }
}
