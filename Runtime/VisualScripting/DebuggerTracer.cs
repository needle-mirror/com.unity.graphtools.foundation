using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine.VisualScripting;

public static class DebuggerTracer
{
    // TODO make that a user option
    const int k_MaxRecordedFrames = 500;

    static Dictionary<GraphReference, GraphTrace> s_PerGraphData;

    public static IEnumerable<KeyValuePair<GraphReference, GraphTrace>> AllGraphs => s_PerGraphData;

    public static void LoadData(Dictionary<GraphReference, GraphTrace> data) => s_PerGraphData = data;

    public struct GraphReference
    {
#pragma warning disable 414
        // used by the implicit equality member
        [UsedImplicitly]
        int m_InstanceId;
#pragma warning restore 414
        public static GraphReference FromId(int graphId) => new GraphReference { m_InstanceId = graphId };

#if UNITY_EDITOR
        public UnityEngine.Object GetGraph() => UnityEditor.EditorUtility.InstanceIDToObject(m_InstanceId);
#endif
    }

    /// <summary>
    /// Contains the recorded data of one graph, stores N frames each containing all entities' framedata
    /// </summary>
    public class GraphTrace
    {
        static FrameData.FrameDataComparer s_KeyComparer = new FrameData.FrameDataComparer();

        CircularBuffer<FrameData> m_PerFrameData;
        public GraphTrace(int maxRecordedFrames = k_MaxRecordedFrames)
        {
            m_PerFrameData = new CircularBuffer<FrameData>(maxRecordedFrames);
        }

        public GraphTrace(IEnumerable<FrameData> frames, int maxRecordedFrames = k_MaxRecordedFrames)
        {
            m_PerFrameData = new CircularBuffer<FrameData>(maxRecordedFrames, frames.ToArray());
        }

        public FrameData GetFrameData(int frame, bool createIfAbsent)
        {
            if (m_PerFrameData.BinarySearch(new FrameData(frame), s_KeyComparer, out var data))
                return data;
            if (createIfAbsent)
                m_PerFrameData.PushBack(data = new FrameData(frame));
            return data;
        }

        public IReadOnlyList<FrameData> AllFrames => m_PerFrameData;
    }

    /// <summary>
    /// Contains the recorded data of one entity for a given graph and a given d frame
    /// </summary>
    [Serializable]
    public class FrameData
    {
        internal class FrameDataComparer : IComparer<FrameData>
        {
            public int Compare(FrameData x, FrameData y)
            {
                if (x == null || y == null)
                    return x == y ? 0 : x == null ? -1 : 1;
                return Comparer<int>.Default.Compare(x.Frame, y.Frame);
            }
        }

        public struct EntityReference
        {
            public int EntityIndex;
            public override string ToString()
            {
                return EntityIndex.ToString();
            }
        }

        public Dictionary<EntityReference, EntityFrameTrace> EntityFrameData;
        public int Frame;

        public FrameData(int frame)
        {
            Frame = frame;
        }

        public FrameData(int frame, Dictionary<EntityReference, EntityFrameTrace> entityFrameData)
        {
            Frame = frame;
            EntityFrameData = entityFrameData;
        }

        public struct EntityDescriptor
        {
            public EntityReference EntityReference { get; }
            public string EntityName { get; }

            public EntityDescriptor(EntityReference entityReference, string entityName)
            {
                EntityReference = entityReference;
                EntityName = entityName;
            }
        }
        public IEnumerable<EntityDescriptor> Entities => EntityFrameData.Select(x => new EntityDescriptor(x.Key, x.Value.EntityName));

        public EntityFrameTrace GetOrCreateEntityFrameTrace(int eIndex, string name)
        {
            if (EntityFrameData == null)
                EntityFrameData = new Dictionary<EntityReference, EntityFrameTrace>();
            var eRef = new EntityReference { EntityIndex = eIndex };
            if (!EntityFrameData.TryGetValue(eRef, out var data))
                EntityFrameData.Add(eRef, data = new EntityFrameTrace { EntityName = name});
            return data;
        }

        public EntityFrameTrace GetExistingEntityFrameTrace(int eIndex)
        {
            if (EntityFrameData == null)
                return null;
            var eRef = new EntityReference { EntityIndex = eIndex };
            EntityFrameData.TryGetValue(eRef, out var data);
            return data;
        }
    }

    /// <summary>
    ///  for one entity and one graph during one frame, store steps and values
    /// </summary>
    public class EntityFrameTrace
    {
        public string EntityName;
        public enum StepType
        {
            None,
            Exception,
        }

        /// <summary>
        /// The recorded value of one node in one graph for one entity during one frame
        /// </summary>
        public struct ValueRecord
        {
            public string readableValue;
            public ulong nodeId1;
            public ulong nodeId2;
        }

        /// <summary>
        /// The recorded execution step in one graph for one entity during one frame. References one node, it's either
        /// a standard step ro an exception
        /// </summary>
        public struct NodeRecord
        {
            public ulong nodeId1;
            public ulong nodeId2;
            public StepType type;
            public string exceptionText;
            public byte progress;
        }

        public List<NodeRecord> steps;
        /// <summary>
        /// Step -> Node+Value tuples
        /// </summary>
        public Dictionary<int, List<ValueRecord>> values;

        int m_NextStepIndex;

        public void SetNextStep(ulong nodeId1, ulong nodeId2, StepType type = StepType.None, string exceptionText = "", byte progress = 0)
        {
            if (steps == null)
                steps = new List<NodeRecord>();

            var nodeRecord = new NodeRecord
            {
                nodeId1 = nodeId1, nodeId2 = nodeId2, type = type, exceptionText = exceptionText,
                progress = progress,
            };

            // skip records already used by Insert() calls
            while (m_NextStepIndex < steps.Count && steps[m_NextStepIndex].nodeId1 != 0 && steps[m_NextStepIndex].nodeId2 != 0)
                m_NextStepIndex++;

            if (m_NextStepIndex < steps.Count)
                steps[m_NextStepIndex] = nodeRecord;
            else
                steps.Add(nodeRecord);
            m_NextStepIndex++;
        }

        /// <summary>
        /// TODO doc / minimize bus factor
        /// </summary>
        /// <param name="stepOffset"></param>
        /// <param name="nodeId1"></param>
        /// <param name="nodeId2"></param>
        /// <param name="type"></param>
        /// <param name="exceptionText"></param>
        /// <param name="progress"></param>
        public void PadAndInsert(int stepOffset, ulong nodeId1, ulong nodeId2, StepType type = StepType.None, string exceptionText = "", byte progress = 0)
        {
            if (steps == null)
                steps = new List<NodeRecord>(stepOffset + 1);

            // skip records already used by Insert() calls
            while (m_NextStepIndex < steps.Count && steps[m_NextStepIndex].nodeId1 != 0 && steps[m_NextStepIndex].nodeId2 != 0)
                m_NextStepIndex++;

            // pad with empty records
            for (int i = steps.Count; i < m_NextStepIndex + stepOffset; i++)
                steps.Add(default);

            steps.Insert(m_NextStepIndex + stepOffset, new NodeRecord { nodeId1 = nodeId1, nodeId2 = nodeId2, type = type, exceptionText = exceptionText, progress = progress });
        }

        [UsedImplicitly]
        public T Record<T>(T value, ulong nodeId1, ulong nodeId2)
        {
            RecordValue(value, nodeId1, nodeId2);
            return value;
        }

        public void RecordValue<T>(T value, ulong nodeId1, ulong nodeId2)
        {
            SetLastCallFrame(nodeId1, nodeId2, 0);
            AddValue(value, nodeId1, nodeId2);
        }

        void AddValue<T>(T value, ulong nodeId1, ulong nodeId2)
        {
            var step = m_NextStepIndex - 1;
            if (values == null)
                values = new Dictionary<int, List<ValueRecord>>();
            if (!values.ContainsKey(step))
                values[step] = new List<ValueRecord>();
            ValueRecord record = new ValueRecord
            {
                readableValue = value is float f ? f.ToString("F") :  value?.ToString() ?? "<null>", nodeId1 = nodeId1, nodeId2 = nodeId2
            };
            values[step].Add(record);
        }

        public void SetLastCallFrame(ulong nodeId1, ulong nodeId2, int stepOffset, byte progress = 0)
        {
            if (stepOffset == 0)
                SetNextStep(nodeId1, nodeId2, progress: progress);
            else
                PadAndInsert(stepOffset, nodeId1, nodeId2, progress: progress);
        }
    }

    public static IEnumerable<FrameData.EntityDescriptor> GetTargets(int frameCount, int vsGraphAssetModelId)
    {
        var graphData = GetGraphData(vsGraphAssetModelId, false);
        return graphData?.GetFrameData(frameCount, false)?.Entities ?? Enumerable.Empty<FrameData.EntityDescriptor>();
    }

    public static GraphTrace GetGraphData(int graphId, bool createIfAbsent)
    {
        if (s_PerGraphData == null)
        {
            if (!createIfAbsent)
                return null;
            s_PerGraphData = new Dictionary<GraphReference, GraphTrace>();
        }

        var graphRef = GraphReference.FromId(graphId);
        if (!s_PerGraphData.TryGetValue(graphRef, out var graphTrace) && createIfAbsent)
            s_PerGraphData.Add(graphRef, graphTrace = new GraphTrace());
        return graphTrace;
    }
}
