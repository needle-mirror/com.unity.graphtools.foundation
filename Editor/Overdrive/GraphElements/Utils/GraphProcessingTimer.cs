using System;
using System.Diagnostics;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class GraphProcessingTimer
    {
        readonly Stopwatch m_IdleTimer;

        public GraphProcessingTimer()
        {
            m_IdleTimer = new Stopwatch();
        }

        public long ElapsedMilliseconds => m_IdleTimer.IsRunning ? m_IdleTimer.ElapsedMilliseconds : 0;
        public bool IsRunning => m_IdleTimer.IsRunning;

        public void Restart(GraphProcessingStateComponent stateComponent)
        {
            m_IdleTimer.Restart();
            stateComponent.GraphProcessingPending = true;
        }

        public void Stop(GraphProcessingStateComponent stateComponent)
        {
            m_IdleTimer.Stop();
            stateComponent.GraphProcessingPending = false;
        }
    }
}
