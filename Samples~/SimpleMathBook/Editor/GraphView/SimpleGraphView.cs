using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.GraphToolsFoundation.Overdrive;
namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook.UI
{
    class SimpleGraphView : GraphView
    {
        private static readonly Vector2 s_CopyOffset = new Vector2(50, 50);
        SimpleGraphViewWindow m_SimpleGraphViewWindow;

        public SimpleGraphViewWindow window
        {
            get { return m_SimpleGraphViewWindow; }
        }

        public SimpleGraphView(SimpleGraphViewWindow simpleGraphViewWindow, bool withWindowedTools, CommandDispatcher store) : base(simpleGraphViewWindow, store, "SimpleGraphView")
        {
            m_SimpleGraphViewWindow = simpleGraphViewWindow;
        }
    }
}
