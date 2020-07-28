using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class StoreDispatchCheck
    {
        int m_LastActionFrame = -1;

        IAction m_LastActionThisFrame;

        IAction m_CurrentAction;

        public int UpdateCounter { get; set; }

        public void BeginDispatch(IAction action, Preferences preferences)
        {
            if (preferences != null && UpdateCounter == m_LastActionFrame &&
                preferences.GetBool(BoolPref.ErrorOnMultipleDispatchesPerFrame))
            {
                Debug.LogError($"Multiple actions dispatched during the same frame (previous one was {m_LastActionThisFrame.GetType().Name}), current: {action.GetType().Name}");
            }

            m_LastActionFrame = UpdateCounter;
            m_LastActionThisFrame = action;

            if (preferences != null && m_CurrentAction != null &&
                preferences.GetBool(BoolPref.ErrorOnRecursiveDispatch))
            {
                Debug.LogError($"Recursive dispatch detected: action {action.GetType().Name} dispatched during {m_CurrentAction.GetType().Name}'s dispatch");
            }

            m_CurrentAction = action;
        }

        public void EndDispatch()
        {
            m_CurrentAction = null;
        }
    }
}
