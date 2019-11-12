using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    class TracingTimeline
    {
        const int k_FrameRectangleWidth = 2;
        const int k_NodeMarkerHeight = 2;
        const int k_FrameRectangleHeight = 8;
        const int k_MinTimeVisibleOnTheRight = 30;

        readonly VseGraphView m_VseGraphView;
        readonly State m_VSWindowState;
        readonly IMGUIContainer m_ImguiContainer;
        TimeArea m_TimeArea;
        AnimEditorOverlay m_Overlay;
        TimelineState m_State;

        public bool Dirty { get; internal set; }

        public TracingTimeline(VseGraphView vseGraphView, State vsWindowState, IMGUIContainer imguiContainer)
        {
            m_VseGraphView = vseGraphView;
            m_VSWindowState = vsWindowState;
            m_ImguiContainer = imguiContainer;
            m_TimeArea = new TimeArea();
            m_Overlay = new AnimEditorOverlay();
            m_State = new TimelineState(m_TimeArea);
            m_Overlay.state = m_State;
        }

        public void SyncVisible()
        {
            if (m_ImguiContainer.style.display.value == DisplayStyle.Flex != m_VSWindowState.EditorDataModel.TracingEnabled)
                m_ImguiContainer.style.display = m_VSWindowState.EditorDataModel.TracingEnabled ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void OnGUI(Rect timeRect)
        {
            // sync timeline and tracing toolbar state both ways
            m_State.CurrentTime = TimelineState.FrameToTime(m_VSWindowState.currentTracingFrame);

            m_Overlay.HandleEvents();
            int timeChangedByTimeline = TimelineState.TimeToFrame(m_State.CurrentTime);
            // force graph update
            if (timeChangedByTimeline != m_VSWindowState.currentTracingFrame)
                m_VSWindowState.currentTracingStep = -1;
            m_VSWindowState.currentTracingFrame = timeChangedByTimeline;

            GUI.BeginGroup(timeRect);

            int graphId = (m_VSWindowState?.AssetModel as Object)?.GetInstanceID() ?? -1;
            DebuggerTracer.GraphTrace trace = DebuggerTracer.GetGraphData(graphId, false);
            if (trace != null && trace.AllFrames.Count > 0)
            {
                float frameDeltaToPixel = m_TimeArea.FrameDeltaToPixel();
                DebuggerTracer.FrameData first = trace.AllFrames[0];
                DebuggerTracer.FrameData last = trace.AllFrames[trace.AllFrames.Count - 1];
                float start = m_TimeArea.FrameToPixel(first.Frame);
                float width = frameDeltaToPixel * Mathf.Max(last.Frame - first.Frame, 1);

                // draw active range
                EditorGUI.DrawRect(new Rect(start,
                    timeRect.yMax - k_FrameRectangleHeight, width, k_FrameRectangleHeight), k_FrameHasDataColor);

                // draw per-node active ranges
                var framesPerNode = IndexFramesPerNode(graphId, trace, first.Frame, last.Frame, m_VSWindowState.currentTracingTarget, out bool invalidated);

                // while recording in unpaused playmode, adjust the timeline to show all data
                // same if the cached data changed (eg. Load a trace dump)
                if (EditorApplication.isPlaying && !EditorApplication.isPaused || invalidated)
                {
                    m_TimeArea.SetShownRange(
                        first.Frame - k_MinTimeVisibleOnTheRight,
                        last.Frame + k_MinTimeVisibleOnTheRight);
                }

                if (framesPerNode != null)
                {
                    INodeModel nodeModelSelected = m_VseGraphView.selection
                        .OfType<IHasGraphElementModel>()
                        .Select(x => x.GraphElementModel)
                        .OfType<INodeModel>()
                        .FirstOrDefault();
                    if (nodeModelSelected != null && framesPerNode.TryGetValue(nodeModelSelected.Guid, out HashSet<(int, int)> frames))
                    {
                        foreach ((int, int)frameInterval in frames)
                        {
                            float xStart = m_TimeArea.FrameToPixel(frameInterval.Item1);
                            float xEnd = m_TimeArea.FrameToPixel(frameInterval.Item2) - frameDeltaToPixel * 0.1f;
                            Rect rect = new Rect(
                                xStart,
                                timeRect.yMin,
                                xEnd - xStart,
                                timeRect.yMax);
                            EditorGUI.DrawRect(rect, k_FrameHasNodeColor);
                        }
                    }
                }
            }
            GUI.EndGroup();


            // time scales
            GUILayout.BeginArea(timeRect);
            m_TimeArea.Draw(timeRect);
            GUILayout.EndArea();

            // playing head
            m_Overlay.OnGUI(timeRect, timeRect);
        }

        struct FramesPerNodeCache
        {
            public Dictionary<SerializableGUID, HashSet<(int, int)>> NodeToFrames;
            public int firstFrame;
            public int lastFrame;
            public int graphId;
            public int EntityId;
        }

        static FramesPerNodeCache s_FramesPerNodeCache;
        static ProfilerMarker s_IndexFramesPerNodeMarker = new ProfilerMarker("IndexFramesPerNode");
        static readonly Color32 k_FrameHasDataColor = new Color32(38, 80, 154, 200);
        static readonly Color32 k_FrameHasNodeColor = new Color32(255, 255, 255, 62);

        static Dictionary<SerializableGUID, HashSet<(int, int)>> IndexFramesPerNode(int graphId, DebuggerTracer.GraphTrace trace, int firstFrame, int lastFrame, int entityId, out bool invalidated)
        {
            invalidated = false;
            if (s_FramesPerNodeCache.graphId == graphId && s_FramesPerNodeCache.firstFrame == firstFrame &&
                s_FramesPerNodeCache.lastFrame == lastFrame && s_FramesPerNodeCache.EntityId == entityId)
                return s_FramesPerNodeCache.NodeToFrames;

            invalidated = true;
            s_IndexFramesPerNodeMarker.Begin();
            s_FramesPerNodeCache.graphId = graphId;
            s_FramesPerNodeCache.firstFrame = firstFrame;
            s_FramesPerNodeCache.lastFrame = lastFrame;
            s_FramesPerNodeCache.EntityId = entityId;

            Dictionary<SerializableGUID, HashSet<(int, int)>> framesPerNode;
            Dictionary<SerializableGUID, SortedSet<int>> framesPerNodeRaw = new Dictionary<SerializableGUID, SortedSet<int>>();
            if (s_FramesPerNodeCache.NodeToFrames == null)
                s_FramesPerNodeCache.NodeToFrames = framesPerNode = new Dictionary<SerializableGUID, HashSet<(int, int)>>();
            else
            {
                framesPerNode = s_FramesPerNodeCache.NodeToFrames;
                framesPerNode.Clear();
            }

            // index individual frames where a node is active
            foreach (var frame in trace.AllFrames)
            {
                var entityTrace = frame.GetExistingEntityFrameTrace(entityId);
                var nodes = entityTrace.steps.Select(s => SerializableGUID.FromParts(s.nodeId1, s.nodeId2));
                foreach (var node in nodes)
                {
                    if (!framesPerNodeRaw.TryGetValue(node, out var frames))
                        framesPerNodeRaw.Add(node, frames = new SortedSet<int>());
                    frames.Add(frame.Frame);
                }
            }

            // compute frame intervals where a node is active
            foreach (var pair in framesPerNodeRaw)
            {
                if (pair.Value.Count == 0)
                    continue;

                if (!framesPerNode.TryGetValue(pair.Key, out var intervals))
                    framesPerNode.Add(pair.Key, intervals = new HashSet<(int, int)>());

                int firstNodeFrame = pair.Value.First();
                // first frame is 3, initial interval is (3,4)
                (int start, int end)curInterval = (firstNodeFrame, firstNodeFrame + 1);
                foreach (var i in pair.Value.Skip(1))
                {
                    // interval was (3,5),  node is not active during frame 5, cur frame is 6
                    if (i != curInterval.end)
                    {
                        intervals.Add(curInterval);
                        curInterval = (i, i + 1);
                    }
                    else // current frame is 4, extend cur interval to (3,5)
                        curInterval.end = i + 1;
                }

                intervals.Add(curInterval);
            }

            s_IndexFramesPerNodeMarker.End();

            return framesPerNode;
        }
    }
}
