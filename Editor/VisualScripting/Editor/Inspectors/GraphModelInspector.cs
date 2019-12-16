using System;
using System.Linq;
using NUnit.Framework.Constraints;

using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    [CustomEditor(typeof(GraphAssetModel), true)]
    class GraphModelInspector : UnityEditor.Editor
    {
        ReorderableList m_ReorderableList;
        public override void OnInspectorGUI()
        {
            VSGraphModel graph = (VSGraphModel)((GraphAssetModel)target)?.GraphModel;
            if (graph == null)
                return;

            EditorGUILayout.LabelField("Stencil Properties");

            EditorGUI.indentLevel++;


            var graphStencil = graph.Stencil;
            graphStencil.OnInspectorGUI();

            EditorGUI.indentLevel--;

            if (graphStencil is IHasOrderedStacks)
            {
                if (m_ReorderableList == null)
                    m_ReorderableList = new ReorderableList(null, typeof(IOrderedStack))
                    {
                        displayAdd = false,
                        displayRemove = false,
                        drawHeaderCallback = rect => GUI.Label(rect, "Execution Order"),
                        drawElementCallback = (rect, index, active, focused) =>
                        {
                            var orderedStack = (IOrderedStack)m_ReorderableList.list[index];
                            GUI.Label(rect, orderedStack.Title);
                        },
                        onReorderCallbackWithDetails = (list, oldIndex, newIndex) =>
                        {
                            for (int i = 0; i < m_ReorderableList.list.Count; i++)
                            {
                                var orderedStack = (IOrderedStack)m_ReorderableList.list[i];
                                orderedStack.Order = i;
                            }

                            graphStencil.RecompilationRequested = true;
                        }
                    };
                m_ReorderableList.list = graphStencil.GetEntryPoints(graph).OfType<IOrderedStack>().OrderBy(x => x.Order).ToList();
                m_ReorderableList.DoLayoutList();
            }

            base.OnInspectorGUI();
        }
    }
}
