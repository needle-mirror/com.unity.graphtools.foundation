using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.EditorCommon.Redux;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Editor.Highlighting;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Editor
{
    public class Blackboard : Experimental.GraphView.Blackboard, IMovable
    {
        public delegate void RebuildCallback(RebuildMode rebuildMode);

        // TODO: Enable when GraphView supports it
        //        [Serializable]
        //        class PersistedProperties
        //        {
        //            public Vector3 position = Vector3.zero;
        //            public Vector2 size = Vector2.zero;
        //            public bool isAutoDimOpacityEnabled = false;
        //        }
        //
        //        PersistedProperties m_PersistedProperties;

        public enum RebuildMode
        {
            BlackboardOnly,
            BlackboardAndGraphView
        }

        public Store Store { get; }

        public List<BlackboardSection> Sections { get; private set; }

        public VseGraphView GraphView => (VseGraphView)graphView;

        IBlackboardProvider m_LastProvider;

        const string k_ClassLibraryTitle = "Blackboard";

        public const string k_PersistenceKey = "Blackboard";

        Button m_AddButton;

        public Blackboard(Store store, VseGraphView graphView, bool windowed)
        {
            Store = store;

            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "Blackboard.uss"));

            AddToClassList("blackboard");

            scrollable = true;
            base.title = k_ClassLibraryTitle;
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
                IGraphModel currentGraphModel = Store.GetState().CurrentGraphModel;
                if (!(currentGraphModel as Object))
                    return;

                if (currentGraphModel == GraphView.UIController.LastGraphModel &&
                    (GraphView.selection.LastOrDefault() is BlackboardField ||
                     GraphView.selection.LastOrDefault() is IVisualScriptingField))
                {
                    ((VSGraphModel)currentGraphModel).LastChanges.RequiresRebuild = true;
                    return;
                }

                RebuildSections();
            };

            var header = this.Query("header").First();
            m_AddButton = header?.Query<Button>("addButton").First();
            if (m_AddButton != null)
                m_AddButton.style.visibility = Visibility.Hidden;

            this.windowed = windowed;
            this.graphView = graphView;
        }

        void DisplayAppropriateSearcher(KeyDownEvent e)
        {
            if (e.keyCode == KeyCode.Space)
                m_LastProvider.DisplayAppropriateSearcher(e.originalMousePosition, this);
        }

        static void OnEditTextRequested(Experimental.GraphView.Blackboard blackboard, VisualElement blackboardField, string newName)
        {
            if (blackboardField is BlackboardVariableField field)
            {
                field.Store.Dispatch(new RenameElementAction((IRenamableModel)field.GraphElementModel, newName));
                field.UpdateTitleFromModel();
            }
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            Selection.selectionChanged += OnSelectionChange;

            RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            RegisterCallback<DragPerformEvent>(OnDragPerform);

            RegisterCallback<KeyDownEvent>(DisplayAppropriateSearcher);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            // ReSharper disable once DelegateSubtraction
            Selection.selectionChanged -= OnSelectionChange;

            UnregisterCallback<DragUpdatedEvent>(OnDragUpdated);
            UnregisterCallback<DragPerformEvent>(OnDragPerform);

            UnregisterCallback<KeyDownEvent>(DisplayAppropriateSearcher);
        }

        void OnDragUpdated(DragUpdatedEvent e)
        {
            IGraphModel currentGraphModel = Store.GetState().CurrentGraphModel;
            if (currentGraphModel == null)
                return;
            var stencil = currentGraphModel.Stencil;
            var dragNDropHandler = stencil.DragNDropHandler;
            dragNDropHandler?.HandleDragUpdated(e, DragNDropContext.Blackboard);
            e.StopPropagation();
        }

        void OnDragPerform(DragPerformEvent e)
        {
            IGraphModel currentGraphModel = Store.GetState().CurrentGraphModel;
            if (currentGraphModel == null)
                return;
            var stencil = currentGraphModel.Stencil;
            var dragNDropHandler = stencil.DragNDropHandler;
            dragNDropHandler?.HandleDragPerform(e, Store, DragNDropContext.Blackboard, this);
            e.StopPropagation();
        }

        void OnSelectionChange()
        {
            IGraphModel currentGraphModel = Store.GetState().CurrentGraphModel;
            if (currentGraphModel == null || !(currentGraphModel.AssetModel as Object))
                return;

            if (currentGraphModel == GraphView.UIController.LastGraphModel &&
                (GraphView.selection.LastOrDefault() is BlackboardField ||
                 GraphView.selection.LastOrDefault() is IVisualScriptingField))
            {
                ((VSGraphModel)currentGraphModel).LastChanges.RequiresRebuild = true;
                return;
            }

            RebuildBlackboard();
        }

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

            IGraphModel currentGraphModel = null;
            if (!(Store.GetState().AssetModel as ScriptableObject))
            {
                title = k_ClassLibraryTitle;
                subTitle = "";
            }
            else
            {
                currentGraphModel = Store.GetState().CurrentGraphModel;
                title = currentGraphModel.FriendlyScriptName;
                subTitle = currentGraphModel.Stencil.GetBlackboardProvider().GetSubTitle();
            }

            var blackboardProvider = currentGraphModel?.Stencil.GetBlackboardProvider();
            if (m_AddButton != null)
                if (blackboardProvider == null || blackboardProvider.CanAddItems == false)
                    m_AddButton.style.visibility = Visibility.Hidden;
        }

        public void UpdatePersistedProperties()
        {
            // TODO: Enable when GraphView supports it
            //            UpdatePersistedProperties(layout.position, layout.size, this.IsAutoDimOpacityEnabled());
        }

        //
        //        void UpdatePersistedProperties(Vector2 position, Vector2 size, bool isAutoDimOpacityEnabled)
        //        {
        //            if (m_PersistedProperties == null)
        //                return;
        //
        //            m_PersistedProperties.position = position;
        //            m_PersistedProperties.size = size;
        //            m_PersistedProperties.isAutoDimOpacityEnabled = isAutoDimOpacityEnabled;
        //
        //            schedule.Execute(DelayedSaveViewData);
        //        }
        //
        //        void DelayedSaveViewData()
        //        {
        //            if (string.IsNullOrEmpty(viewDataKey))
        //            {
        //                var graphModel = (VSGraphModel)m_Store.GetState()?.currentGraphModel;
        //                SetPersistenceKeyFromGraphModel(graphModel);
        //            }
        //
        //            SaveViewData();
        //        }
        //
        //        public override void OnViewDataReady()
        //        {
        //            schedule.Execute(DelayedOnViewDataReady);
        //        }

        //        void DelayedOnViewDataReady()
        //        {
        //            base.OnViewDataReady();
        //
        //            string key = GetFullHierarchicalViewDataKey();
        //            m_PersistedProperties = GetOrCreateViewData<PersistedProperties>(m_PersistedProperties, key);
        //            UpdateProperties(m_PersistedProperties);
        //        }

        public void UpdatePinning()
        {
            UpdatePersistedProperties();
        }

        public bool NeedStoreDispatch => false;

        // TODO: Enable when GraphView supports it
        //        void UpdateProperties(PersistedProperties properties)
        //        {
        //            var newPosition = properties.position;
        //            var newSize = properties.size;
        //            var newAutoDimOpacityEnabled = properties.isAutoDimOpacityEnabled;
        //
        //            float validateFloat = newPosition.x + newPosition.y + newPosition.z + newSize.x + newSize.y;
        //            if (float.IsInfinity(validateFloat) || float.IsNaN(validateFloat) || newSize.x < float.Epsilon || newSize.y < float.Epsilon)
        //                return;
        //
        //            SetPosition(new Rect(newPosition, newSize));
        //
        //            if ((this.IsAutoDimOpacityEnabled() && !newAutoDimOpacityEnabled) ||
        //                (!this.IsAutoDimOpacityEnabled() && newAutoDimOpacityEnabled))
        //                this.ToggleAutoDimOpacity(VisualElementExtensions.StartingOpacity.Min);
        //
        //            UpdatePersistedProperties(newPosition, newSize, newAutoDimOpacityEnabled);
        //        }

        void OnAddItemRequested(Experimental.GraphView.Blackboard blackboard)
        {
            var currentGraphModel = Store.GetState().CurrentGraphModel;
            currentGraphModel.Stencil.GetBlackboardProvider().AddItemRequested(Store, (IAction)null);
        }

        void OnMoveItemRequested(Experimental.GraphView.Blackboard blackboard, int index, VisualElement field)
        {
            // TODO: Workaround to prevent moving item above a BlackboardThisField, as all check code is executed
            // within UnityEditor.Experimental.GraphView.BlackboardSection in private or internal functions
            bool hasBlackboardThisField = (blackboard as Blackboard)?.Sections?[0]?.Children()
                ?.Any(x => x is BlackboardThisField) ?? false;
            if (index == 0 && hasBlackboardThisField)
                return;

            var currentGraphModel = Store.GetState().CurrentGraphModel;
            currentGraphModel.Stencil.GetBlackboardProvider().MoveItemRequested(Store, index, field);
        }

        public void Rebuild(RebuildMode rebuildMode)
        {
            IBlackboardProvider blackboardProvider =
                ((VSGraphModel)Store.GetState().CurrentGraphModel).Stencil.GetBlackboardProvider();
            if (Sections == null || m_LastProvider != blackboardProvider)
            {
                m_LastProvider = blackboardProvider;
                ClearContents();
                Clear();
                Sections = blackboardProvider?.CreateSections().ToList();
                Sections?.ForEach(Add);
            }

            if (rebuildMode == RebuildMode.BlackboardAndGraphView)
                Store.Dispatch(new RefreshUIAction(UpdateFlags.GraphTopology));
            else
                RebuildBlackboard();
        }

        void RebuildBlackboard()
        {
            var currentGraphModel = Store.GetState().CurrentGraphModel;
            title = currentGraphModel.FriendlyScriptName;

            subTitle = currentGraphModel.Stencil.GetBlackboardProvider().GetSubTitle();

            var blackboardProvider = currentGraphModel.Stencil.GetBlackboardProvider();
            if (m_AddButton != null)
                if (!blackboardProvider.CanAddItems)
                    m_AddButton.style.visibility = Visibility.Hidden;
                else
                    m_AddButton.style.visibility = StyleKeyword.Null;

            RebuildSections();

            var editorDataModel = Store.GetState().EditorDataModel;
            IGraphElementModel elementModelToRename = editorDataModel?.ElementModelToRename;
            if (elementModelToRename != null)
            {
                IRenamable elementToRename = GraphVariables.OfType<IRenamable>()
                    .FirstOrDefault(x => ReferenceEquals(x.GraphElementModel, elementModelToRename));
                if (elementToRename != null)
                    GraphView.UIController.ElementToRename = elementToRename;
            }

            GraphView.HighlightGraphElements();
        }

        public void RestoreSelectionForElement(GraphElement element)
        {
            var editorDataModel = Store.GetState().EditorDataModel;

            // Select upon creation...
            if (element is IHasGraphElementModel hasElementGraphModel && editorDataModel.ShouldSelectElementUponCreation(hasElementGraphModel))
                element.Select(GraphView, true);

            // ...or regular selection
            // TODO: This bypasses the problems with selection in GraphView when selection contains
            // non layered elements (like Blackboard fields for example)
            {
                if (!GraphView.PersistentSelectionContainsElement(element) ||
                    GraphView.selection.Contains(element) && element.selected)
                    return;

                element.selected = true;
                if (!GraphView.selection.Contains(element))
                    selection.Add(element);
                element.OnSelected();

                // To ensure that the selected GraphElement gets unselected if it is removed from the GraphView.
                element.RegisterCallback<DetachFromPanelEvent>(OnSelectedElementDetachedFromPanel);

                element.MarkDirtyRepaint();
            }
        }

        void OnSelectedElementDetachedFromPanel(DetachFromPanelEvent evt)
        {
            var selectable = evt.target as ISelectable;
            if (!(selectable is GraphElement graphElement))
                return;

            graphElement.selected = false;
            selection.Remove(selectable);
            graphElement.OnUnselected();
            graphElement.UnregisterCallback<DetachFromPanelEvent>(OnSelectedElementDetachedFromPanel);
            graphElement.MarkDirtyRepaint();
        }

        public List<IHighlightable> GraphVariables { get; } = new List<IHighlightable>();

        void RebuildSections()
        {
            if (Sections != null)
            {
                var currentGraphModel = Store.GetState().CurrentGraphModel;
                var blackboardProvider = currentGraphModel.Stencil.GetBlackboardProvider();
                blackboardProvider.RebuildSections(this);
            }
        }

        void OnContextualMenuEvent(ContextualMenuPopulateEvent evt)
        {
            GraphView.BuildContextualMenu(evt);
        }

        public void NotifyTopologyChange(IGraphModel graphModel)
        {
            SetPersistenceKeyFromGraphModel(graphModel);
        }

        void SetPersistenceKeyFromGraphModel(IGraphModel graphModel)
        {
            viewDataKey = graphModel?.GetAssetPath() + "__" + k_PersistenceKey;
        }
    }
}
