using System;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class Blackboard : GraphElements.Blackboard
    {
        public new VseGraphView GraphView => base.GraphView as VseGraphView;


        public const string k_PersistenceKey = "Blackboard";

        Button m_AddButton;

        public Blackboard(Store store, VseGraphView graphView, bool windowed) : base(store, graphView)
        {
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "Blackboard.uss"));

            AddToClassList("blackboard");

            scrollable = true;
            title = k_ClassLibraryTitle;
            subTitle = "";

            viewDataKey = string.Empty;

            addItemRequested = OnAddItemRequested;
            moveItemRequested = OnMoveItemRequested;

            // TODO 0.5: hack - we have two conflicting renaming systems
            // the blackboard one seems to win
            // for 0.4, just rewire it to dispatch the same action as ours
            editTextRequested = OnEditTextRequested;

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            this.AddManipulator(new ContextualMenuManipulator(OnContextualMenuEvent));

            graphView.OnSelectionChangedCallback += s =>
            {
                IGTFGraphModel currentGraphModel = Store.GetState().CurrentGraphModel;
                if (!(currentGraphModel as Object))
                    return;

                if (currentGraphModel == GraphView.UIController.LastGraphModel &&
                    (GraphView.Selection.LastOrDefault() is BlackboardField ||
                     GraphView.Selection.LastOrDefault() is IVisualScriptingField))
                {
                    currentGraphModel.LastChanges.RequiresRebuild = true;
                    return;
                }

                RebuildSections();
            };

            var header = this.Query("header").First();
            m_AddButton = header?.Query<Button>("addButton").First();
            if (m_AddButton != null)
                m_AddButton.style.visibility = Visibility.Hidden;

            this.windowed = windowed;
        }

        static void OnEditTextRequested(GraphElements.Blackboard blackboard, VisualElement blackboardField, string newName)
        {
            if (blackboardField is BlackboardVariableField field)
            {
                field.Store.Dispatch(new RenameElementAction((UnityEditor.GraphToolsFoundation.Overdrive.Model.IRenamable)field.Model, newName));
                field.UpdateTitleFromModel();
            }
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            UnityEditor.Selection.selectionChanged += OnSelectionChange;

            RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            RegisterCallback<DragPerformEvent>(OnDragPerform);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            // ReSharper disable once DelegateSubtraction
            UnityEditor.Selection.selectionChanged -= OnSelectionChange;

            UnregisterCallback<DragUpdatedEvent>(OnDragUpdated);
            UnregisterCallback<DragPerformEvent>(OnDragPerform);
        }

        void OnDragUpdated(DragUpdatedEvent e)
        {
            IGTFGraphModel currentGraphModel = Store.GetState().CurrentGraphModel;
            if (currentGraphModel == null)
                return;
            var stencil = currentGraphModel.Stencil;
            var dragNDropHandler = stencil.DragNDropHandler;
            dragNDropHandler?.HandleDragUpdated(e, DragNDropContext.Blackboard);
            e.StopPropagation();
        }

        void OnDragPerform(DragPerformEvent e)
        {
            IGTFGraphModel currentGraphModel = Store.GetState().CurrentGraphModel;
            if (currentGraphModel == null)
                return;
            var stencil = currentGraphModel.Stencil;
            var dragNDropHandler = stencil.DragNDropHandler;
            dragNDropHandler?.HandleDragPerform(e, Store, DragNDropContext.Blackboard, this);
            e.StopPropagation();
        }

        void OnSelectionChange()
        {
            IGTFGraphModel currentGraphModel = Store.GetState().CurrentGraphModel;
            if (currentGraphModel == null || !(currentGraphModel.AssetModel as Object))
                return;

            if (currentGraphModel == GraphView.UIController.LastGraphModel &&
                (GraphView.Selection.LastOrDefault() is BlackboardField ||
                 GraphView.Selection.LastOrDefault() is IVisualScriptingField))
            {
                currentGraphModel.LastChanges.RequiresRebuild = true;
                return;
            }

            RebuildBlackboard();
        }

        void OnAddItemRequested(GraphElements.Blackboard blackboard)
        {
            var currentGraphModel = Store.GetState().CurrentGraphModel;
            currentGraphModel.Stencil.GetBlackboardProvider().AddItemRequested(Store, (IAction)null);
        }

        void OnMoveItemRequested(GraphElements.Blackboard blackboard, int index, VisualElement field)
        {
            var currentGraphModel = Store.GetState().CurrentGraphModel;
            currentGraphModel.Stencil.GetBlackboardProvider().MoveItemRequested(Store, index, field);
        }

        protected override void RebuildBlackboard()
        {
            base.RebuildBlackboard();

            IGTFGraphElementModel elementModelToRename = Store.GetState().EditorDataModel?.ElementModelToRename;
            if (elementModelToRename != null)
            {
                IRenamable elementToRename = GraphVariables.OfType<IRenamable>()
                    .FirstOrDefault(x => ReferenceEquals(x.Model, elementModelToRename));
                if (elementToRename != null)
                    GraphView.UIController.ElementToRename = elementToRename;
            }

            GraphView.HighlightGraphElements();
        }

        void OnContextualMenuEvent(ContextualMenuPopulateEvent evt)
        {
            GraphView.BuildContextualMenu(evt);
        }

        public void NotifyTopologyChange(IGTFGraphModel graphModel)
        {
            SetPersistenceKeyFromGraphModel(graphModel);
        }

        void SetPersistenceKeyFromGraphModel(IGTFGraphModel graphModel)
        {
            viewDataKey = graphModel?.GetAssetPath() + "__" + k_PersistenceKey;
        }
    }
}
