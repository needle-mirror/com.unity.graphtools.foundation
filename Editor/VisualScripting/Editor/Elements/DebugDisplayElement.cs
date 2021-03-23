// please don't commit with the following enabled, only toggle when needed
// #define WITH_VS_DEBUG_TOOLING

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.VisualScripting.Editor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace VisualScripting.Editor.Elements
{
    [PublicAPI]
    public class DebugDisplayElement : ImmediateModeElement
    {
        readonly VseGraphView m_GraphView;

        static Action s_ApplyWireMaterialDelegate;

#if WITH_VS_DEBUG_TOOLING
        public static bool Allowed => true;
#else
        public static bool Allowed => false;
#endif

        struct DebugRectData
        {
            public Rect rect;
            public Color color;
        }

        List<DebugRectData> m_Crosses;
        List<DebugRectData> m_Boxes;

        static DebugDisplayElement()
        {
            MethodInfo methodInfo = typeof(HandleUtility).GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Single(
                m =>
                    m.Name == "ApplyWireMaterial" &&
                    m.GetGenericArguments().Length == 0 &&
                    m.GetParameters().Length == 0);
            Assert.IsNotNull(methodInfo);
            s_ApplyWireMaterialDelegate = (Action)methodInfo.CreateDelegate(typeof(Action));
        }

        public DebugDisplayElement(VseGraphView graphView)
        {
            m_GraphView = graphView;

            pickingMode = PickingMode.Ignore;

            m_Crosses = new List<DebugRectData>();
            m_Boxes = new List<DebugRectData>();

            AddCross(Rect.MinMaxRect(-10, -10, 10, 10), Color.green);
        }

        public void ClearDebug()
        {
            m_Boxes.Clear();
            m_Crosses.Clear();
        }

        protected override void ImmediateRepaint()
        {
            DrawEverything();
        }

        [Conditional("WITH_VS_DEBUG_TOOLING")]
        void DrawEverything()
        {
            if (m_GraphView.ShowDebug)
            {
                s_ApplyWireMaterialDelegate();

                GL.Begin(GL.LINES);
                foreach (DebugRectData data in m_Boxes)
                {
                    DrawBox(data.rect, data.color);
                }

                foreach (DebugRectData data in m_Crosses)
                {
                    DrawCross(data.rect, data.color);
                }

                GL.End();
            }
        }

        [Conditional("WITH_VS_DEBUG_TOOLING")]
        public void AddCross(Rect r, Color c)
        {
            m_Crosses.Add(new DebugRectData { rect = r, color = c });
        }

        [Conditional("WITH_VS_DEBUG_TOOLING")]
        public void AddBox(Rect r, Color c)
        {
            m_Boxes.Add(new DebugRectData { rect = r, color = c });
        }

        [Conditional("WITH_VS_DEBUG_TOOLING")]
        static void DrawCross(Rect r, Color c)
        {
            GL.Color(c);

            GL.Vertex3(r.xMin, r.yMin, 0);
            GL.Vertex3(r.xMax, r.yMax, 0);

            GL.Vertex3(r.xMax, r.yMin, 0);
            GL.Vertex3(r.xMin, r.yMax, 0);
        }

        [Conditional("WITH_VS_DEBUG_TOOLING")]
        static void DrawBox(Rect r, Color c)
        {
            r.xMin++;
            r.xMax--;
            r.yMin++;
            r.yMax--;

            GL.Color(c);

            GL.Vertex3(r.xMin, r.yMin, 0);
            GL.Vertex3(r.xMax, r.yMin, 0);

            GL.Vertex3(r.xMax, r.yMin, 0);
            GL.Vertex3(r.xMax, r.yMax, 0);

            GL.Vertex3(r.xMax, r.yMax, 0);
            GL.Vertex3(r.xMin, r.yMax, 0);

            GL.Vertex3(r.xMin, r.yMax, 0);
            GL.Vertex3(r.xMin, r.yMin, 0);
        }
    }
}
