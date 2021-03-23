using System;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;

namespace UnityEditor.VisualScripting.Model
{
    [SearcherItem(typeof(ClassStencil), SearcherContext.Stack, "Control Flow/Return")]
    [BranchedNode]
    [Serializable]
    public class ReturnNodeModel : NodeModel, IHasMainInputPort
    {
        const string k_Title = "Return";

        public override string Title => k_Title;

        PortModel m_InputPort;
        public IPortModel InputPort => m_InputPort;

        protected override void OnDefineNode()
        {
            var returnType = ParentStackModel?.OwningFunctionModel?.ReturnType;
            m_InputPort = returnType != null && returnType.Value.IsValid && returnType != TypeHandle.Void
                ? AddDataInputPort("value", returnType.Value)
                : null;
        }
    }
}
