using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class Blackboard : GraphElement, ISelection
    {
        public static new readonly string ussClassName = "ge-blackboard";
        public static readonly string windowedModifierUssClassName = ussClassName.WithUssModifier("windowed");

        public static readonly string blackboardHeaderPartName = "header";
        public static readonly string blackboardContentPartName = "content";

        public static readonly string persistenceKey = "Blackboard";

        protected Dragger m_Dragger;
        protected ScrollView m_ScrollView;
        protected VisualElement m_ContentContainer;

        public override VisualElement contentContainer => m_ContentContainer;

        // ISelection implementation
        public List<ISelectableGraphElement> Selection => GraphView?.Selection;

        bool m_Windowed;

        protected Dragger Dragger
        {
            get => m_Dragger;
            set
            {
                if (m_Windowed)
                    this.RemoveManipulator(m_Dragger);
                m_Dragger = value;
                if (!m_Windowed)
                    this.AddManipulator(m_Dragger);
            }
        }

        public bool Windowed
        {
            set
            {
                if (m_Windowed == value) return;

                if (value)
                {
                    AddToClassList(windowedModifierUssClassName);
                    this.RemoveManipulator(m_Dragger);
                }
                else
                {
                    RemoveFromClassList(windowedModifierUssClassName);
                    this.AddManipulator(m_Dragger);
                }
                m_Windowed = value;
            }
        }

        // PF: remove Is..
        public override bool IsMovable()
        {
            return !m_Windowed;
        }

        public override bool IsResizable()
        {
            return true;
        }

        public Blackboard()
        {
            Dragger = new Dragger { ClampToParentEdges = true };

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

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            RegisterCallback<PromptSearcherEvent>(OnPromptSearcher);

            focusable = true;
        }

        protected override void BuildPartList()
        {
            base.BuildPartList();

            PartList.AppendPart(BlackboardHeaderPart.Create(blackboardHeaderPartName, Model, this, ussClassName));
            PartList.AppendPart(BlackboardSectionListPart.Create(blackboardContentPartName, Model, this, ussClassName));
        }

        protected override void BuildElementUI()
        {
            base.BuildElementUI();

            m_ScrollView = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            m_ContentContainer = new VisualElement { name = "content-container" };

            hierarchy.Add(m_ScrollView);
            m_ScrollView.Add(m_ContentContainer);
            ResizeRestriction = ResizeRestriction.None; // As both the width and height can be changed by the user using a resizer

            hierarchy.Add(new Resizer());
        }

        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            // Blackboard is not really a graph element. Leaving the class meddles with the rows selection border.
            RemoveFromClassList(GraphElement.ussClassName);
            AddToClassList(ussClassName);
            this.AddStylesheet("Blackboard.uss");
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

        public List<IHighlightable> Highlightables { get; } = new List<IHighlightable>();

        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            var selectedModels = Selection.OfType<IModelUI>().Select(e => e.Model).ToArray();
            if (selectedModels.Length > 0)
            {
                evt.menu.AppendAction("Delete", menuAction =>
                {
                    CommandDispatcher.Dispatch(new DeleteElementsCommand(selectedModels));
                }, eventBase => DropdownMenuAction.Status.Normal);
            }
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            RegisterCallback<KeyDownEvent>(OnKeyDownEvent);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            UnregisterCallback<KeyDownEvent>(OnKeyDownEvent);
        }

        void OnKeyDownEvent(KeyDownEvent e)
        {
            if ((e.keyCode == KeyCode.F2 && Application.platform != RuntimePlatform.OSXEditor) ||
                ((e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) && Application.platform == RuntimePlatform.OSXEditor))
            {
                var renamableSelection = Selection.OfType<GraphElement>().Where(x => x.IsRenamable()).ToList();
                var lastSelectedItem = renamableSelection.LastOrDefault();
                if (lastSelectedItem != null)
                {
                    if (renamableSelection.Count > 1)
                    {
                        ClearSelection();
                        AddToSelection(lastSelectedItem);
                    }

                    lastSelectedItem.Rename();

                    e.StopPropagation();
                }
            }
            else if (e.keyCode == KeyCode.Space)
            {
                using (var promptSearcherEvent = PromptSearcherEvent.GetPooled(e.originalMousePosition))
                {
                    var target = panel.Pick(e.originalMousePosition);
                    promptSearcherEvent.target = target;
                    SendEvent(promptSearcherEvent);
                }
            }
        }

        void OnPromptSearcher(PromptSearcherEvent e)
        {
            var graphModel = (Model as IBlackboardGraphModel)?.GraphModel;

            if (graphModel == null)
            {
                return;
            }

            SearcherService.ShowVariableTypes(
                graphModel.Stencil,
                e.MenuPosition,
                (t, i) =>
                {
                    CommandDispatcher.Dispatch(new CreateGraphVariableDeclarationCommand
                    {
                        VariableName = "newVariable",
                        TypeHandle = t,
                        ModifierFlags = ModifierFlags.None,
                        IsExposed = true
                    });
                });

            e.StopPropagation();
        }
    }
}
