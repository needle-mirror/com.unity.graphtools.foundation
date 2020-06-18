#if DISABLE_SIMPLE_MATH_TESTS
using System;
using System.Linq;
using UnityEditor.UIElements;
using Unity.GraphElements;
using Unity.GraphToolsFoundation.Model;
using Unity.GraphToolsFoundation.Runtime.Model;
using Unity.GraphToolsFoundations.Bridge;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.UsingDataModel.NoPresenters
{
    class SimpleStackNode : StackNode
    {
        private VisualElement m_Header;
        private Label m_TitleItem;
        private TextField m_TitleEditor;
        EnumField m_OperationControl;
        bool m_EditTitleCancelled = false;

        public SimpleStackNode(MathStackNode node)
        {
            var visualTree = Resources.Load("SimpleStackNodeHeader") as VisualTreeAsset;

            m_Header = visualTree.Instantiate();

            m_TitleItem = m_Header.Q<Label>(name: "titleLabel");
            m_TitleItem.AddToClassList("label");

            m_TitleEditor = m_Header.Q(name: "titleField") as TextField;

            m_TitleEditor.AddToClassList("textfield");
            m_TitleEditor.visible = false;

            m_TitleEditor.RegisterCallback<FocusOutEvent>(e => { OnEditTitleFinished(); });
            m_TitleEditor.Q("unity-text-input").RegisterCallback<KeyDownEvent>(OnKeyPressed);


            headerContainer.Add(m_Header);

            m_OperationControl = m_Header.Q<EnumField>(name: "operationField");
            m_OperationControl.Init(MathStackNode.Operation.Addition);
            m_OperationControl.value = node.currentOperation;
            m_OperationControl.RegisterCallback<ChangeEvent<Enum>>(v =>
            {
                node.currentOperation = (MathStackNode.Operation)v.newValue;

                MathNode mathNode = userData as MathNode;

                if (mathNode != null && mathNode.mathBook != null)
                {
                    mathNode.mathBook.inputOutputs.ComputeOutputs();
                }
            });

            var inputPort = InstantiatePort(Orientation.Vertical, Direction.Input, PortCapacity.Single, typeof(float));
            var outputPort = InstantiatePort(Orientation.Vertical, Direction.Output, PortCapacity.Single, typeof(float));

            inputPort.portName = "";
            outputPort.portName = "";

            inputContainer.Add(inputPort);
            outputContainer.Add(outputPort);

            RegisterCallback<MouseDownEvent>(OnMouseUpEvent);

            userData = node;
            viewDataKey = node.nodeID.ToString();
            title = node.name;
            inputPort.userData = node;
            outputPort.userData = node;
        }

        protected override bool AcceptsElement(GraphElement element, ref int proposedIndex, int maxIndex)
        {
            if (element == null || (element.userData is MathResult && this.Children().Any(e => e.userData is MathResult)))
                return false;

            if (proposedIndex != -1)
            {
                proposedIndex = GetEffectiveIndex(element, proposedIndex, maxIndex);
            }

            return true;
        }

        private int GetEffectiveIndex(GraphElement element, int index, int maxIndex)
        {
            if (element.userData is MathResult)
            {
                return maxIndex;
            }
            else if (this.Children().Any(e => e.userData is MathResult) && index == maxIndex)
            {
                return index - 1;
            }

            return index;
        }

        private void OnMouseUpEvent(MouseDownEvent e)
        {
            if (e.clickCount == 2 && IsRenamable())
            {
                if (m_TitleItem.ContainsPoint(this.ChangeCoordinatesTo(m_TitleItem, e.localMousePosition)))
                {
                    m_TitleEditor.value = title;
                    m_TitleEditor.visible = true;
                    m_TitleItem.visible = false;
                    // Workaround: Wait for a delay before giving focus to the newly shown title editor
                    this.schedule.Execute(GiveFocusToTitleEditor).StartingIn(300);
                }
            }
        }

        private void GiveFocusToTitleEditor()
        {
            m_TitleEditor.SelectAll();
            m_TitleEditor.Q(TextField.textInputUssName).Focus();
        }

        public override string title
        {
            get { return m_TitleItem.text; }
            set
            {
                if (!m_TitleItem.Equals(value))
                {
                    m_TitleItem.text = value;

                    MathNode mathNode = userData as MathNode;

                    if (mathNode != null)
                    {
                        mathNode.name = value;
                    }
                }
            }
        }

        private void OnKeyPressed(KeyDownEvent e)
        {
            switch (e.keyCode)
            {
                case KeyCode.Escape:
                    m_EditTitleCancelled = true;
                    m_TitleEditor.Blur();
                    break;
                case KeyCode.Return:
                    m_TitleEditor.Blur();
                    break;
                default:
                    break;
            }
        }

        private void OnEditTitleFinished()
        {
            m_TitleItem.visible = true;
            m_TitleEditor.visible = false;

            if (!m_EditTitleCancelled)
            {
                if (title != m_TitleEditor.text)
                {
                    title = m_TitleEditor.text;
                }
            }

            m_EditTitleCancelled = false;
        }
    }
}
#endif
