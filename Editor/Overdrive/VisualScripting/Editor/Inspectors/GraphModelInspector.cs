using System;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [CustomEditor(typeof(GraphAssetModel), true)]
    class GraphModelInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            VSGraphModel graph = (VSGraphModel)((GraphAssetModel)target)?.GraphModel;
            if (graph == null)
                return;

            EditorGUILayout.LabelField("Stencil Properties");

            EditorGUI.indentLevel++;

            var graphStencil = graph.Stencil;
            if (graphStencil != null)
                graphStencil.OnInspectorGUI();

            EditorGUI.indentLevel--;

            base.OnInspectorGUI();
        }
    }
}
