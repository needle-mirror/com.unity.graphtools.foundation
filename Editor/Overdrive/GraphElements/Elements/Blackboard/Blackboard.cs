using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class Blackboard : GraphElement, ISelection, IMovableGraphElement
    {
        private VisualElement m_MainContainer;
        private VisualElement m_Root;
        private Label m_TitleLabel;
        private Label m_SubTitleLabel;
        private ScrollView m_ScrollView;
        private VisualElement m_ContentContainer;
        private VisualElement m_HeaderItem;
        private Button m_AddButton;
        private bool m_Scrollable = true;

        private Dragger m_Dragger;
        private GraphView m_GraphView;
        public GraphView graphView
        {
            get
            {
                if (!windowed && m_GraphView == null)
                    m_GraphView = GetFirstAncestorOfType<GraphView>();
                return m_GraphView;
            }

            set
            {
                if (!windowed)
                    return;
                m_GraphView = value;
            }
        }

        internal static readonly string StyleSheetPath = "Blackboard.uss";

        public Action<Blackboard> addItemRequested { get; set; }
        public Action<Blackboard, int, VisualElement> moveItemRequested { get; set; }
        public Action<Blackboard, VisualElement, string> editTextRequested { get; set; }

        // ISelection implementation
        public List<ISelectableGraphElement> selection
        {
            get
            {
                return graphView?.selection;
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
            get { return m_Windowed; }
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
                    resizeRestriction = ResizeRestriction.None; // As both the width and height can be changed by the user using a resizer

                    AddToClassList("scrollable");
                }
                else
                {
                    if (m_ScrollView != null)
                    {
                        // Remove the sections container from the scrollview and add it to the content item
                        resizeRestriction = ResizeRestriction.FlexDirection; // As the height is automatically computed from the content but the width can be changed by the user using a resizer
                        m_ScrollView.RemoveFromHierarchy();
                        m_ContentContainer.RemoveFromHierarchy();
                        m_Root.Add(m_ContentContainer);
                    }
                    RemoveFromClassList("scrollable");
                }
            }
        }

        public Blackboard(GraphView associatedGraphView = null)
        {
            var tpl = GraphElementsHelper.LoadUXML("Blackboard.uxml");
            this.AddStylesheet(StyleSheetPath);

            m_MainContainer = tpl.Instantiate();
            m_MainContainer.AddToClassList("mainContainer");

            m_Root = m_MainContainer.Q("content");

            m_HeaderItem = m_MainContainer.Q("header");
            m_HeaderItem.AddToClassList("blackboardHeader");

            m_AddButton = m_MainContainer.Q(name: "addButton") as Button;
            m_AddButton.clickable.clicked += () => {
                if (addItemRequested != null)
                {
                    addItemRequested(this);
                }
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

            m_GraphView = associatedGraphView;
            focusable = true;
        }

        public virtual void AddToSelection(ISelectableGraphElement selectable)
        {
            graphView?.AddToSelection(selectable);
        }

        public virtual void RemoveFromSelection(ISelectableGraphElement selectable)
        {
            graphView?.RemoveFromSelection(selectable);
        }

        public virtual void ClearSelection()
        {
            graphView?.ClearSelection();
        }

        private void OnValidateCommand(ValidateCommandEvent evt)
        {
            graphView?.OnValidateCommand(evt);
        }

        private void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            graphView?.OnExecuteCommand(evt);
        }

        public virtual void UpdatePinning()
        {
        }

        public virtual bool IsMovable => false;
    }
}
