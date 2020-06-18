using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
//    [CustomEditor(typeof(AbstractNodeAsset), true)]
    class NodeModelInspector : GraphElementModelInspector
    {
        bool m_InputsCollapsed = true;
        bool m_OutputsCollapsed = true;

        protected override void GraphElementInspectorGUI(Action refreshUI)
        {
//            if (target is AbstractNodeAsset asset)
//            {
//                var node = asset.Model;
//                node.HasUserColor = EditorGUILayout.Toggle("Set Custom Color", node.HasUserColor);
//                if (node.HasUserColor)
//                    node.Color = EditorGUILayout.ColorField("Node Color", node.Color);
//
//                DisplayPorts(node);
//            }
        }

        protected void DisplayPorts(INodeModel node)
        {
            GUI.enabled = false;

            m_InputsCollapsed = EditorGUILayout.Foldout(m_InputsCollapsed, "Inputs");
            if (m_InputsCollapsed)
                DisplayPorts(node.VSGraphModel.Stencil, node.InputsByDisplayOrder);

            m_OutputsCollapsed = EditorGUILayout.Foldout(m_OutputsCollapsed, "Outputs");
            if (m_OutputsCollapsed)
                DisplayPorts(node.VSGraphModel.Stencil, node.OutputsByDisplayOrder);

            GUI.enabled = true;
        }

        static void DisplayPorts(Stencil stencil, IEnumerable<IPortModel> ports)
        {
            EditorGUI.indentLevel++;
            foreach (var port in ports)
            {
                string details = port.PortType + " ( " + port.DataTypeHandle.GetMetadata(stencil).FriendlyName + " )";
                EditorGUILayout.LabelField(port.UniqueId, details);
                if (Unsupported.IsDeveloperMode())
                {
                    EditorGUI.indentLevel++;
                    foreach (IEdgeModel edgeModel in port.VSGraphModel.GetEdgesConnections(port))
                    {
                        int edgeIndex = edgeModel.VSGraphModel.EdgeModels.IndexOf(edgeModel);
                        EditorGUILayout.LabelField(edgeIndex.ToString(), edgeModel.OutputPortModel.ToString());
                        EditorGUILayout.LabelField("to", edgeModel.InputPortModel.ToString());
                    }
                    EditorGUI.indentLevel--;
                }
            }
            EditorGUI.indentLevel--;
        }
    }
}
