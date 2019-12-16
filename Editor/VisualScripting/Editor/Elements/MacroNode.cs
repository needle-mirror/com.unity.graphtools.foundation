using System;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Editor.Renamable;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    class MacroNode : Node, IRenamable, IDoubleClickable
    {
        MacroRefNodeModel m_MacroModel;
        VisualElement m_TitleElement;
        public MacroNode(MacroRefNodeModel model, Store store, GraphView graphView) : base(model, store, graphView)
        {
            m_MacroModel = model;
            AddToClassList("macro");

            m_TitleElement = this.MandatoryQ("title");
            m_TitleElement.pickingMode = PickingMode.Position;

            this.EnableRename();

            var clickable = new Clickable(DoAction);
            clickable.activators.Clear();
            clickable.activators.Add(
                new ManipulatorActivationFilter {button = MouseButton.LeftMouse, clickCount = 2});
            this.AddManipulator(clickable);

            IGraphElementModel elementModelToRename = m_Store.GetState().EditorDataModel.ElementModelToRename;
            if (elementModelToRename as MacroRefNodeModel == model)
                ((VseGraphView)m_GraphView).UIController.ElementToRename = this;
        }

        public Store Store => m_Store;

        void DoAction()
        {
            if (m_MacroModel.GraphAssetModel != null)
                Store.Dispatch(new LoadGraphAssetAction(
                    m_MacroModel.GraphAssetModel.GraphModel.GetAssetPath(), true, LoadGraphAssetAction.Type.PushOnStack));
        }

        public string TitleValue => m_MacroModel.Title;
        TextField m_TitleTextfield;
        public VisualElement TitleEditor => m_TitleTextfield ?? (m_TitleTextfield = new TextField { name = "titleEditor", isDelayed = true });
        public VisualElement TitleElement => m_TitleElement;
        public override bool IsRenamable() => base.IsRenamable() && m_MacroModel.Capabilities.HasFlag(CapabilityFlags.Renamable);
        public bool IsFramable() => true;
        public bool EditTitleCancelled { get; set; } = false;

        public RenameDelegate RenameDelegate => null;
    }
}
