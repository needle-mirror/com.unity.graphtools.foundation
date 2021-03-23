using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class CommandDispatchCheck
    {
        int m_LastCommandFrame = -1;

        Command m_LastCommandThisFrame;

        Command m_CurrentCommand;

        public int UpdateCounter { get; set; }

        public void BeginDispatch(Command command, Preferences preferences)
        {
            if (preferences != null && UpdateCounter == m_LastCommandFrame &&
                preferences.GetBool(BoolPref.ErrorOnMultipleDispatchesPerFrame))
            {
                Debug.LogError($"Multiple commands dispatched during the same frame (previous one was {m_LastCommandThisFrame.GetType().Name}), current: {command.GetType().Name}");
            }

            m_LastCommandFrame = UpdateCounter;
            m_LastCommandThisFrame = command;

            if (preferences != null && m_CurrentCommand != null &&
                preferences.GetBool(BoolPref.ErrorOnRecursiveDispatch))
            {
                Debug.LogError($"Recursive dispatch detected: command {command.GetType().Name} dispatched during {m_CurrentCommand.GetType().Name}'s dispatch");
            }

            m_CurrentCommand = command;
        }

        public void EndDispatch()
        {
            m_CurrentCommand = null;
        }
    }
}
