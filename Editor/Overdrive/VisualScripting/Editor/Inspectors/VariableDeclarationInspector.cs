using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
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
