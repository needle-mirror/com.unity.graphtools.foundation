using System;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

public static class GraphViewTestHelpers
{
    public class TimerEventSchedulerWrapper : IDisposable
    {
        readonly GraphView m_GraphView;

        internal TimerEventSchedulerWrapper(GraphView graphView)
        {
            m_GraphView = graphView;
            Panel.TimeSinceStartup = () => TimeSinceStartup;
        }

        public long TimeSinceStartup { get; set; }

        public void Dispose()
        {
            Panel.TimeSinceStartup = null;
        }

        public void UpdateScheduledEvents()
        {
            TimerEventScheduler s = (TimerEventScheduler)m_GraphView.elementPanel.scheduler;
            s.UpdateScheduledEvents();
        }
    }

    public static TimerEventSchedulerWrapper CreateTimerEventSchedulerWrapper(this GraphView graphView)
    {
        return new TimerEventSchedulerWrapper(graphView);
    }

    public static int SelectionDraggerPanInterval => SelectionDragger.k_PanInterval;

    public class EditorWindows
    {
        public static void ShowInspector()
        {
            EditorWindow.GetWindow<InspectorWindow>().Show(true);
        }
    }
}
