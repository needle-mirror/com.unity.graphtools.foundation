using System;
using UnityEditor.VisualScripting.Model;

namespace UnityEditor.VisualScripting.Editor
{
    [CustomEditor(typeof(VariableDeclarationModel), true)]
    class VariableDeclarationInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Not supported in Inspector, please use Blackboard to edit variables.", MessageType.Info);
        }
    }
}
