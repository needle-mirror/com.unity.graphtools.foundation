using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Assertions;

namespace UnityEditor.VisualScripting.Editor.Plugins
{
    public class TraceDump
    {
        string m_GraphPath;
        public DebuggerTracer.FrameData[] FrameData;
        const ushort k_Version = 2;

        public TraceDump(string graphPath, DebuggerTracer.FrameData[] frameData)
        {
            m_GraphPath = graphPath;
            FrameData = frameData;
        }

        public static TraceDump Deserialize(BinaryReader reader)
        {
            var version = reader.ReadInt16();
            Assert.AreEqual(k_Version, version);
            var path = reader.ReadString();
            var frameCount = reader.ReadInt32();
            var frameData = new DebuggerTracer.FrameData[frameCount];
            TraceDump graphData = new TraceDump(path, frameData);
            for (int i = 0; i < frameCount; i++)
            {
                frameData[i] = new DebuggerTracer.FrameData(reader.ReadInt32());
                var entityCount = reader.ReadInt32();
                var entityData = new Dictionary<DebuggerTracer.FrameData.EntityReference, DebuggerTracer.EntityFrameTrace>(entityCount);
                frameData[i].EntityFrameData = entityData;

                for (int j = 0; j < entityCount; j++)
                {
                    DebuggerTracer.FrameData.EntityReference entityRef = new DebuggerTracer.FrameData.EntityReference { EntityIndex = reader.ReadInt32() };
                    var entityFrameTrace = new DebuggerTracer.EntityFrameTrace { EntityName = reader.ReadString() };
                    {
                        entityFrameTrace.steps = new List<DebuggerTracer.EntityFrameTrace.NodeRecord>(reader.ReadInt32());
                        for (int k = 0; k < entityFrameTrace.steps.Capacity; k++)
                        {
                            entityFrameTrace.steps.Add(new DebuggerTracer.EntityFrameTrace.NodeRecord
                            {
                                nodeId1 = reader.ReadUInt64(),
                                nodeId2 = reader.ReadUInt64(),
                                progress = reader.ReadByte(),
                                type = reader.ReadBoolean() ? DebuggerTracer.EntityFrameTrace.StepType.Exception : DebuggerTracer.EntityFrameTrace.StepType.None,
                            });
                        }
                    }
                    {
                        var valueCount = reader.ReadInt32();
                        entityFrameTrace.values =
                            new Dictionary<int, List<DebuggerTracer.EntityFrameTrace.ValueRecord>>(valueCount);
                        for (int k = 0; k < valueCount; k++)
                        {
                            var step = reader.ReadInt32();
                            List<DebuggerTracer.EntityFrameTrace.ValueRecord> values = new List<DebuggerTracer.EntityFrameTrace.ValueRecord>(reader.ReadInt32());

                            for (int l = 0; l < values.Capacity; l++)
                            {
                                values.Add(new DebuggerTracer.EntityFrameTrace.ValueRecord
                                {
                                    nodeId1 = reader.ReadUInt64(),
                                    nodeId2 = reader.ReadUInt64(),
                                    readableValue = reader.ReadString(),
                                });
                            }

                            entityFrameTrace.values.Add(step, values);
                        }
                    }
                    entityData.Add(entityRef, entityFrameTrace);
                }
            }

            return graphData;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(k_Version);
            writer.Write(m_GraphPath);
            writer.Write(FrameData.Length);
            for (int i = 0; i < FrameData.Length; i++)
            {
                var frameData = FrameData[i];
                writer.Write(frameData.Frame);
                writer.Write(frameData.EntityFrameData.Count);

                foreach (var entityFrameTrace in frameData.EntityFrameData)
                {
                    writer.Write(entityFrameTrace.Key.EntityIndex);
                    writer.Write(entityFrameTrace.Value.EntityName ?? String.Empty);
                    {
                        var steps = entityFrameTrace.Value.steps;
                        writer.Write(steps.Count);
                        foreach (var nodeRecord in steps)
                        {
                            writer.Write(nodeRecord.nodeId1);
                            writer.Write(nodeRecord.nodeId2);
                            writer.Write(nodeRecord.progress);
                            writer.Write(nodeRecord.type != DebuggerTracer.EntityFrameTrace.StepType.None);
                        }
                    }
                    {
                        var values = entityFrameTrace.Value.values;
                        writer.Write(values?.Count ?? 0);
                        if (values != null)
                            foreach (var keyValuePair in values)
                            {
                                writer.Write(keyValuePair.Key);
                                writer.Write(keyValuePair.Value.Count);

                                foreach (var valueRecord in keyValuePair.Value)
                                {
                                    writer.Write(valueRecord.nodeId1);
                                    writer.Write(valueRecord.nodeId2);
                                    writer.Write(valueRecord.readableValue);
                                }
                            }
                    }
                }
            }
        }
    }
}
