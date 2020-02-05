using System;
using UnityEditor.EditorCommon.Redux;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    public class Store : Store<State>
    {
        public enum Options
        {
            None,
            TrackUndoRedo,
        }

        readonly Options m_Options;

        UndoRedoTraversal m_UndoRedoTraversal;

        IAction m_CurrentAction;

        IAction m_LastActionThisFrame;

        int m_LastActionFrame = -1;

        public Store(State initialState = null, Options options = Options.None)
            : base(initialState)
        {
            m_Options = options;

            if (m_Options == Options.TrackUndoRedo)
            {
                Undo.undoRedoPerformed += UndoRedoPerformed;
            }

            RegisterReducers();
        }

        public void RegisterReducers()
        {
            // Register reducers.
            UIReducers.Register(this);
            EditorReducers.Register(this);
            GraphAssetReducers.Register(this);
            GraphReducers.Register(this);
            StackReducers.Register(this);
            NodeReducers.Register(this);
#if UNITY_2020_1_OR_NEWER
            PlacematReducers.Register(this);
#endif
            EdgeReducers.Register(this);
            VariableReducers.Register(this);
            PropertiesReducers.Register(this);
            StickyNoteReducers.Register(this);
        }

        public override void Dispatch<TAction>(TAction action)
        {
            VSPreferences vsPreferences = GetState().Preferences;

            if (vsPreferences != null && vsPreferences.GetBool(VSPreferences.BoolPref.LogAllDispatchedActions))
                Debug.Log(action);
            int currentFrame = GetState()?.EditorDataModel == null ? -1 : GetState().EditorDataModel.UpdateCounter;
            if (vsPreferences != null && currentFrame == m_LastActionFrame &&
                vsPreferences.GetBool(VSPreferences.BoolPref.ErrorOnMultipleDispatchesPerFrame))
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

            if (vsPreferences != null && m_CurrentAction != null &&
                vsPreferences.GetBool(VSPreferences.BoolPref.ErrorOnRecursiveDispatch))
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
            if (m_Options == Options.TrackUndoRedo)
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
                if (GetState().AssetModel.GraphModel is VSGraphModel vsGraphModel && vsGraphModel != null)
                    m_UndoRedoTraversal.VisitGraph(vsGraphModel);
            }
        }

        protected override void PreStateChanged()
        {
            CheckForTopologyChanges();
        }

        void ClearRegistrations()
        {
            m_Reducers.Clear();
        }

        protected override void PostStateChanged()
        {
            State state = GetState();

            state.EditorDataModel?.SetUpdateFlag(UpdateFlags.None);
            state.RegisterReducers(this, ClearRegistrations);
        }

        protected override void PreDispatchAction(IAction action)
        {
            SaveDispatchedActionName(action);
        }

        void CheckForTopologyChanges()
        {
            State state = GetState();

            IGraphModel currentGraphModel = state.CurrentGraphModel;
            IEditorDataModel editorDataModel = state.EditorDataModel;

            if (editorDataModel != null && currentGraphModel.HasAnyTopologyChange())
                editorDataModel.SetUpdateFlag(editorDataModel.UpdateFlags | UpdateFlags.GraphTopology);

            if (editorDataModel != null && currentGraphModel?.LastChanges?.RequiresRebuild == true)
                editorDataModel.SetUpdateFlag(editorDataModel.UpdateFlags | UpdateFlags.RequestRebuild);
        }

        void SaveDispatchedActionName<TAction>(TAction action) where TAction : IAction
        {
            State state = GetState();

            state.LastDispatchedActionName = action.GetType().Name;

            IGraphModel vsStateCurrentGraphModel = state.CurrentGraphModel;
            vsStateCurrentGraphModel?.ResetChanges();
            state.lastActionUIRebuildType = State.UIRebuildType.None;
        }
    }
}
