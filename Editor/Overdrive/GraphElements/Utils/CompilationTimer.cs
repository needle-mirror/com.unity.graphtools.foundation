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

        public void Restart(IEditorDataModel editorDataModel)
        {
            m_IdleTimer.Restart();
            editorDataModel.CompilationPending = true;
        }

        public void Stop(IEditorDataModel editorDataModel)
        {
            m_IdleTimer.Stop();
            editorDataModel.CompilationPending = false;
        }
    }
}
