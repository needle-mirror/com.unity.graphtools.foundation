using System;
using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class TracingDataModel
    {
        public int CurrentTracingTarget = -1;
        public int CurrentTracingFrame;
        public int CurrentTracingStep;
        public int MaxTracingStep;

        public TracingDataModel(int currentTracingStep)
        {
            CurrentTracingStep = currentTracingStep;
        }

        /// <summary>
        /// Stores the list of steps for the current graph, frame and target tuple
        /// </summary>
        public List<TracingStep> DebuggingData { get; set; }
    }
}
