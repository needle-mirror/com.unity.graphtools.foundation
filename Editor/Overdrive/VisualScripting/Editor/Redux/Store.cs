using System;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class Store : GraphElements.Store
    {
        UndoRedoTraversal m_UndoRedoTraversal;

        IAction m_CurrentAction;

        IAction m_LastActionThisFrame;

        int m_LastActionFrame = -1;

        public Store(State initialState)
            : base(initialState)
        {
            Undo.undoRedoPerformed += UndoRedoPerformed;
            RegisterReducers();
        }

        public void RegisterReducers()
        {
            // Register reducers.
            UIReducers.Register(this);
            GraphAssetReducers.Register(this);
            GraphReducers.Register(this);
            NodeReducers.Register(this);
            PlacematReducers.Register(this);
            PortalReducers.Register(this);
            EdgeReducers.Register(this);
            VariableReducers.Register(this);
            StickyNoteReducers.Register(this);
        }

        public override void Dispatch<TAction>(TAction action)
        {
            Preferences preferences = GetState().Preferences;

            if (preferences != null && preferences.GetBool(BoolPref.LogAllDispatchedActions))
                Debug.Log(action);
            int currentFrame = GetState()?.EditorDataModel == null ? -1 : GetState().EditorDataModel.UpdateCounter;
            if (preferences != null && currentFrame == m_LastActionFrame &&
                preferences.GetBool(BoolPref.ErrorOnMultipleDispatchesPerFrame))
            {
                // TODO: Specific case for a non-model specific action, possibly triggered by a callback that is unaware of the store's current state;
                //       About RefreshUIAction: maybe this is not a good idea to update the UI via an action, as it has nothing
                //       to do with the model.  Problem is, currently, the Store reacts on state changes and is responsible
                //       of updating the UI accordingly.  This UI update loop could be moved elsewhere and detach itself
                //       from the editor model.  Same goes for PanToNode action.
                if (!(action is RefreshUIAction || action is PanToNodeAction))
                    Debug.LogError($"Multiple actions dispatched during the same frame (previous one was {m_LastActionThisFrame.GetType().Name}), current: {action.GetType().Name}");
            }

            m_LastActionFrame = currentFrame;
            m_LastActionThisFrame = action;

            if (preferences != null && m_CurrentAction != null &&
                preferences.GetBool(BoolPref.ErrorOnRecursiveDispatch))
            {
                // TODO: Same check here, see comments above
                if (!(action is RefreshUIAction))
                    Debug.LogError($"Recursive dispatch detected: action {action.GetType().Name} dispatched during {m_CurrentAction.GetType().Name}'s dispatch");
            }

            m_CurrentAction = action;
            try
            {
                base.Dispatch(action);
            }
            finally
            {
                m_CurrentAction = null;
            }
        }

        public override void Dispose()
        {
            // ReSharper disable once DelegateSubtraction
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            base.Dispose();
        }

        void UndoRedoPerformed()
        {
            GraphModel graphModel = GetState().AssetModel?.GraphModel as GraphModel;
            if (graphModel != null)
            {
                graphModel.UndoRedoPerformed();
                if (m_UndoRedoTraversal == null)
                    m_UndoRedoTraversal = new UndoRedoTraversal();
                m_UndoRedoTraversal.VisitGraph(GetState().AssetModel.GraphModel);
            }
        }

        protected override void PreStateChanged()
        {
            CheckForTopologyChanges();
        }

        protected override void PostStateChanged()
        {
            State state = GetState<State>();

            state.EditorDataModel?.SetUpdateFlag(UpdateFlags.None);
            state.RegisterReducers(this, ClearReducers);
        }

        protected override void PreDispatchAction(IAction action)
        {
            SaveDispatchedActionName(action);
        }

        void CheckForTopologyChanges()
        {
            State state = GetState<State>();

            IGraphModel currentGraphModel = state.CurrentGraphModel;
            IEditorDataModel editorDataModel = state.EditorDataModel;

            if (editorDataModel != null && currentGraphModel.HasAnyTopologyChange())
                editorDataModel.SetUpdateFlag(editorDataModel.UpdateFlags | UpdateFlags.GraphTopology);

            if (editorDataModel != null && currentGraphModel?.LastChanges?.RequiresRebuild == true)
                editorDataModel.SetUpdateFlag(editorDataModel.UpdateFlags | UpdateFlags.RequestRebuild);
        }

        void SaveDispatchedActionName<TAction>(TAction action) where TAction : IAction
        {
            State state = GetState<State>();

            state.LastDispatchedActionName = action.GetType().Name;

            IGraphModel vsStateCurrentGraphModel = state.CurrentGraphModel;
            vsStateCurrentGraphModel?.ResetChanges();
            state.LastActionUIRebuildType = Overdrive.State.UIRebuildType.None;
        }
    }
}
