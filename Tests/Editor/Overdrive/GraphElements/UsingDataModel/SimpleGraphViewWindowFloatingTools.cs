#if DISABLE_SIMPLE_MATH_TESTS
using System;
using UnityEditor;
using UnityEngine;

namespace Editor.UsingDataModel.NoPresenters
{
    class SimpleGraphViewWindowFloatingTools : SimpleGraphViewWindow
    {
        protected override bool withWindowedTools => true;

        [MenuItem("GraphView/Simple Graph With Floating Tools")]
        public static void ShowWindow()
        {
            ShowGraphViewWindowWithTools<SimpleGraphViewWindowFloatingTools>();
        }

        public override void OnEnable()
        {
            base.OnEnable();

            graphView.name = "MathBook Floating Tools";
            graphView.viewDataKey = "MathBook Floating Tools";

            titleContent.text = "Simple Graph With Floating Tools";
        }
    }
}
#endif
