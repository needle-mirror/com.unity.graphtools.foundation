using System;
using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [Serializable]
    public class TracingStateComponent : AssetViewStateComponent
    {
        public bool TracingEnabled;
        public int CurrentTracingTarget = -1;
        public int CurrentTracingFrame;
        public int CurrentTracingStep;
        public int MaxTracingStep;

        public TracingStateComponent()
        {
            CurrentTracingStep = -1;
        }

        /// <summary>
        /// Stores the list of steps for the current graph, frame and target tuple
        /// </summary>
        public List<TracingStep> DebuggingData { get; set; }
    }
}
