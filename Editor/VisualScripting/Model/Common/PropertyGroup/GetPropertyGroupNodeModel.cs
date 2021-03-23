using System;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.Model.Stencils;

namespace UnityEditor.VisualScripting.Model
{
    [SearcherItem(typeof(ClassStencil), SearcherContext.Graph, k_Title)]
    [Serializable]
    public class GetPropertyGroupNodeModel : PropertyGroupBaseNodeModel
    {
        public const string k_Title = "Get Property";

        public override string Title => k_Title;

        protected override Direction MemberPortDirection => Direction.Output;
    }
}
