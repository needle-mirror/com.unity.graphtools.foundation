using System;
using JetBrains.Annotations;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Editor.Renamable;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    [PublicAPI]
    public class RenamableNode : Node, IRenamable
    {
        TextField m_TitleTextfield;

        public RenamableNode(INodeModel model, Store store, GraphView graphView)
            : base(model, store, graphView)
        {
            if (model is IRenamableModel)
                this.EnableRename();
        }

        public Store Store => m_Store;
        public string TitleValue => model.Title;
        public VisualElement TitleEditor => m_TitleTextfield ?? (m_TitleTextfield = new TextField { name = "titleEditor", isDelayed = true });
        public VisualElement TitleElement => titleContainer;
        public bool IsFramable() => true;
        public bool EditTitleCancelled { get; set; }
        public RenameDelegate RenameDelegate => null;
    }
}
