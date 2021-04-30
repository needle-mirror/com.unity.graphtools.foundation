namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [CustomEditor(typeof(GraphAssetModel), true)]
    class GraphModelInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var graph = ((IGraphAssetModel)target)?.GraphModel;
            if (graph != null)
            {
                EditorGUILayout.LabelField("Stencil Properties");

                EditorGUI.indentLevel++;
                ((Stencil)graph.Stencil)?.OnInspectorGUI();
                EditorGUI.indentLevel--;
            }

            base.OnInspectorGUI();
        }
    }
}
