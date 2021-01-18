using System;
using System.Diagnostics;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class CompilationTimer
    {
        readonly Stopwatch m_IdleTimer;

        public CompilationTimer()
        {
            m_IdleTimer = new Stopwatch();
        }

        public long ElapsedMilliseconds => m_IdleTimer.IsRunning ? m_IdleTimer.ElapsedMilliseconds : 0;
        public bool IsRunning => m_IdleTimer.IsRunning;

        public void Restart(CompilationStateComponent stateComponent)
        {
            m_IdleTimer.Restart();
            stateComponent.CompilationPending = true;
        }

        public void Stop(CompilationStateComponent stateComponent)
        {
            m_IdleTimer.Stop();
            stateComponent.CompilationPending = false;
        }
    }
}
