using System;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine.UIElements;

namespace Packages.VisualScripting.Editor.Elements
{
    public class RenamableLabel : GraphElement, IRenamable
    {
        Label m_Label;

        public IGraphElementModel GraphElementModel { get; }
        public Store Store { get; }
        public string TitleValue => GraphElementModel.ToString();

        TextField m_TextField;

        public VisualElement TitleEditor => m_TextField ?? (m_TextField = new TextField { name = "titleEditor", isDelayed = true });
        public VisualElement TitleElement => m_Label;

        public bool IsFramable() => false;

        public bool EditTitleCancelled { get; set; } = false;

        public RenameDelegate RenameDelegate => OpenTextEditor;

        VseGraphView m_GraphView;
        Action<string> m_RenameAction;

        VseGraphView GraphView => m_GraphView ?? (m_GraphView = GetFirstAncestorOfType<VseGraphView>());

        public RenamableLabel(IGraphElementModel graphElementModel, string text, Store store, Action<string> renameAction)
        {
            name = "renamableLabel";

            GraphElementModel = graphElementModel;
            Store = store;

            m_RenameAction = renameAction;

            ClearClassList();

            m_Label = new Label() { text = text, name = "label" };
            Add(m_Label);

            m_TextField = new TextField { name = "textField", isDelayed = true };
            m_TextField.style.display = DisplayStyle.None;
            Add(m_TextField);

            var textInput = m_TextField.Q(TextField.textInputUssName);
            textInput.RegisterCallback<FocusOutEvent>(_ => OnEditTextFinished());

            RegisterCallback<MouseDownEvent>(OnMouseDownEvent);

            capabilities |= Capabilities.Renamable;

            this.AddManipulator(new ContextualMenuManipulator(OnContextualMenuEvent));
        }

        void OnContextualMenuEvent(ContextualMenuPopulateEvent evt)
        {
            GraphView.BuildContextualMenu(evt);
        }

        void OnEditTextFinished()
        {
            m_TextField.style.display = DisplayStyle.None;

            if (m_Label.text != m_TextField.text)
            {
                m_RenameAction?.Invoke(m_TextField.text);
            }
        }

        void OpenTextEditor()
        {
            m_TextField.SetValueWithoutNotify(m_Label.text);
            m_TextField.style.display = DisplayStyle.Flex;
            m_TextField.Q(TextField.textInputUssName).Focus();
            m_TextField.SelectAll();
        }

        void OnMouseDownEvent(MouseDownEvent e)
        {
            if ((e.clickCount == 2) && e.button == (int)MouseButton.LeftMouse && IsRenamable())
            {
                OpenTextEditor();
                e.PreventDefault();
                e.StopImmediatePropagation();
            }
        }
    }
}
