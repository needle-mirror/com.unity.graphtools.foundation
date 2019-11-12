using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine.VisualScripting;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests
{
    [Category("Tracing")]
    class FrameTests
    {
        static void AssertNodeIds(DebuggerTracer.EntityFrameTrace frame, params ulong[] ids)
        {
            Assert.That(frame.steps.Count, Is.EqualTo(ids.Length));
            for (int i = 0; i < ids.Length; i++)
            {
                Assert.That(frame.steps[i].nodeId1, Is.EqualTo(ids[i]));
            }
        }

        [Test]
        public void TestCircularBufferIsWorking()
        {
            DebuggerTracer.EntityFrameTrace recorder = new DebuggerTracer.EntityFrameTrace();
            Assert.That(recorder.values?.ContainsKey(1) ?? false, Is.False);
            recorder.Record("a", 1, 0);

            Check(1, "a");

            Assert.That(recorder.values?.ContainsKey(2) ?? false, Is.False);
            recorder.Record("b", 2, 0);
            Check(2, "b");

            Assert.That(recorder.values?.ContainsKey(3) ?? false, Is.False);
            recorder.Record("c", 3, 0);
            Check(3, "c");

            void Check(ulong anodeId, object value)
            {
                var valueRecord = recorder.values.Last().Value.Last();
                Assert.That(valueRecord.nodeId1, Is.EqualTo(anodeId));
                Assert.That(valueRecord.readableValue, Is.EqualTo(value.ToString()));
            }
        }

        [Test]
        public void TestCircularBufferHasRightValues()
        {
            DebuggerTracer.GraphTrace recorder = new DebuggerTracer.GraphTrace(10);
            const int entityId = 0;

            for (int i = 0; i < 20; i++)
            {
                DebuggerTracer.FrameData frameData = recorder.GetFrameData(i, true);
                DebuggerTracer.EntityFrameTrace entityData = frameData.GetOrCreateEntityFrameTrace(entityId, "");
                entityData.RecordValue(i, 0, 0);
            }
            for (int i = 0; i < 20; i++)
            {
                if (i < 10)
                    Assert.That(recorder.GetFrameData(i, false), Is.Null);
                else
                    Assert.That(recorder.GetFrameData(i, false).GetExistingEntityFrameTrace(entityId).values[0].Single().readableValue, Is.EqualTo(i.ToString()));
            }
        }

        [Test]
        public void TestFrame()
        {
            DebuggerTracer.EntityFrameTrace frame = new DebuggerTracer.EntityFrameTrace();

            // _ _ 3
            frame.PadAndInsert(stepOffset: 2, nodeId1: 3, nodeId2: 3);
            AssertNodeIds(frame, 0, 0, 3);

            // 1 _ 3
            frame.SetNextStep(1, 1);
            AssertNodeIds(frame, 1, 0, 3);

            // 1 2 3
            frame.SetNextStep(2, 2);
            AssertNodeIds(frame, 1, 2, 3);

            // 1 2 3 4
            frame.SetNextStep(4, 4);
            AssertNodeIds(frame, 1, 2, 3, 4);

            // 1 2 3 4 _ _ 7
            frame.PadAndInsert(2, 7, 7);
            AssertNodeIds(frame, 1, 2, 3, 4, 0, 0, 7);

            // 1 2 3 4 5 _ 7
            frame.SetNextStep(5, 5);
            AssertNodeIds(frame, 1, 2, 3, 4, 5, 0, 7);

            // 1 2 3 4 5 6 7
            frame.SetNextStep(6, 6);
            AssertNodeIds(frame, 1, 2, 3, 4, 5, 6, 7);
        }
    }
}
