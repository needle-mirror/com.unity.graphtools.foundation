using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [CustomEditor(typeof(GraphAssetModel), true)]
    class GraphModelInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            var graph = ((IGTFGraphAssetModel)target)?.GraphModel;
            if (graph != null)
            {
                EditorGUILayout.LabelField("Stencil Properties");

                EditorGUI.indentLevel++;
                graph.Stencil?.OnInspectorGUI();
                EditorGUI.indentLevel--;
            }

            base.OnInspectorGUI();
        }
    }
}
