using System;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
//    [CustomEditor(typeof(ConstantNodeAsset<>), true)]
    class ConstantNodeModelInspector : NodeModelInspector
    {
        GUIContent m_GUIContent;
        protected override bool DoDefaultInspector => false;

        protected override void GraphElementInspectorGUI(Action refreshUI)
        {
            if (m_GUIContent == null)
                m_GUIContent = new GUIContent("value");

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

//            var graph = (target as AbstractNodeAsset)?.Model.GraphModel;
//            if (graph != null)
//                ConstantEditorGUI(serializedObject, m_GUIContent, graph.Stencil, ConstantEditorMode.AllEditors, refreshUI);

            serializedObject.ApplyModifiedProperties();
        }

        public enum ConstantEditorMode { ValueOnly, AllEditors }

        public static void ConstantEditorGUI(SerializedObject o, GUIContent label, Stencil stencil,
            ConstantEditorMode mode = ConstantEditorMode.ValueOnly, Action onChange = null)
        {
//            if (!(o.targetObject is AbstractNodeAsset asset))
//                return;

//            switch (asset.Model)
//            {
//                case ConstantNodeModel constantNodeModel when constantNodeModel.IsLocked:
//                    return;
//
//                case EnumConstantNodeModel enumModel:
//                {
//                    if (mode != ConstantEditorMode.ValueOnly)
//                    {
//                        var filter = new SearcherFilter(SearcherContext.Type).WithEnums(stencil);
//                        stencil.TypeEditor(enumModel.value.EnumType,
//                            (type, index) =>
//                            {
//                                enumModel.value.EnumType = type;
//                                onChange?.Invoke();
//                            }, filter);
//                    }
//                    enumModel.value.Value = Convert.ToInt32(EditorGUILayout.EnumPopup("Value", enumModel.EnumValue));
//                    break;
//                }
//
//                default:
//                    EditorGUILayout.PropertyField(o.FindProperty("m_NodeModel.value"), label, true);
//                    break;
//            }
        }
    }
}
