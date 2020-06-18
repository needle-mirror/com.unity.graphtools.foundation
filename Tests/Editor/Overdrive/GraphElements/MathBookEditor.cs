using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MathBook))]
public class MathBookEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MathBook mathBook = target as MathBook;

        foreach (MathBookField input in mathBook.inputOutputs.inputs)
        {
            input.value = EditorGUILayout.FloatField(new GUIContent(input.name, input.toolTip), input.value);
        }
        foreach (MathBookField output in mathBook.inputOutputs.outputs)
        {
            EditorGUILayout.LabelField(new GUIContent(output.name, output.toolTip), new GUIContent(output.value.ToString()));
        }
    }
}
