using System;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [CustomEditor(typeof(IGraphElementModel), true)]
    class GraphElementModelInspector : UnityEditor.Editor
    {
        protected virtual bool DoDefaultInspector => true;

        public sealed override void OnInspectorGUI()
        {
            if (DoDefaultInspector)
                base.OnInspectorGUI();

            EditorGUI.BeginChangeCheck();
            GraphElementInspectorGUI(RefreshUI);
            if (EditorGUI.EndChangeCheck())
                RefreshUI();
        }

        protected virtual void GraphElementInspectorGUI(Action refreshUI)
        {
        }

        static void RefreshUI()
        {
            var window = EditorWindow.GetWindow<VseWindow>();
            if (window != null)
                window.RefreshUI(UpdateFlags.All);
        }
    }
}
