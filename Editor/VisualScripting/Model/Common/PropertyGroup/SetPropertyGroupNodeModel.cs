using System;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.Model.Stencils;

namespace UnityEditor.VisualScripting.Model
{
    [SearcherItem(typeof(ClassStencil), SearcherContext.Stack, k_Title)]
    [Serializable]
    public class SetPropertyGroupNodeModel : PropertyGroupBaseNodeModel
    {
        public const string k_Title = "Set Property";

        public override string Title => k_Title;

        protected override Direction MemberPortDirection => Direction.Input;
    }
}
