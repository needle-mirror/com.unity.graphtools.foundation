using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    /// <summary>
    ///  This class is used to support derived NodeModels' CustomEditors in the inspector when a Node is selected by
    ///  bridging the NodeModel with a ScriptableObject derived proxy required by the CustomEditor to retrieve the
    ///  selection
    /// </summary>
    /// <example>
    ///  This sample code shows how to define a CustomEditor for a specific Node model type
    /// <code>
    /// <![CDATA[public class MyNodeModelProxy : NodeModelProxy<MyNodeModel> {}]]>
    ///  [CustomEditor(typeof(MyNodeModelProxy), true)]
    ///  internal class MyNodeBaseCustomEditor : UnityEditor.Editor { ... }
    /// </code>
    /// </example>
    public class NodeModelProxy<T> : ScriptableObject, INodeModelProxy where T : IGraphElementModel
    {
        public ScriptableObject ScriptableObject() { return this;}

        public void SetModel(IGraphElementModel model) { Model = (T)model; }

        [SerializeReference]
        public T Model;
    }
}
