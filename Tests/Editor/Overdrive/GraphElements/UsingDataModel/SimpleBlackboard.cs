#if DISABLE_SIMPLE_MATH_TESTS
using System;
using UnityEditor;
using Unity.GraphElements;
using Unity.GraphToolsFoundations.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.UsingDataModel.NoPresenters
{
    class SimpleBlackboardField : BlackboardField
    {
        public MathBookField field { get { return userData as MathBookField; } }

        public SimpleBlackboardField(MathBookField field) :  base(null, "", "Float")
        {
            userData = field;
            UpdateData();
            field.changed += (e, c) => UpdateData();
        }

        void UpdateData()
        {
            text = field.name;
            icon = field.exposed ? GraphViewStaticBridge.LoadIconRequired("GraphView/Nodes/BlackboardFieldExposed.png") : null;
        }
    }

    class SimpleBlackboard : Blackboard
    {
        BlackboardSection m_InSection;
        BlackboardSection m_OutSection;

        public MathBook mathBook { get; }

        public SimpleBlackboard(MathBook book, GraphView associatedGraphView) :
            base(associatedGraphView)
        {
            scrollable = true;
            title = "Math Book";

            m_InSection = new BlackboardSection();
            m_OutSection = new BlackboardSection();

            m_InSection.title = "Inputs";
            m_OutSection.title = "Outputs";

            Add(m_InSection);
            Add(m_OutSection);

            addItemRequested = OnAddItemRequested;
            editTextRequested = OnEditTextRequested;
            moveItemRequested = OnMoveItemRequested;

            mathBook = book;
        }

        void AddField(MathBookField field)
        {
            var item = new SimpleBlackboardField(field);
            var section = field.direction == MathBookField.Direction.Input ? m_InSection : m_OutSection;
            section.Add(new BlackboardRow(item, new BlackboardFieldPropertyView(field)));
        }

        void ClearFields()
        {
            m_InSection.Clear();
            m_OutSection.Clear();
        }

        void OnAddItemRequested(Blackboard blackboard)
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(EditorGUIUtility.TrTextContent("Add Input"), false, OnMenuItemTriggered, MathBookField.Direction.Input);
            menu.AddItem(EditorGUIUtility.TrTextContent("Add Output"), false, OnMenuItemTriggered, MathBookField.Direction.Output);

            menu.ShowAsContext();
        }

        void OnEditTextRequested(Blackboard blackboard, VisualElement field, string name)
        {
            MathBookField bookField = field.userData as MathBookField;

            if (bookField.name != name)
                bookField.name = mathBook.inputOutputs.GenerateUniqueFieldName(name);
        }

        void OnMoveItemRequested(Blackboard blackboard, int index, VisualElement field)
        {
            MathBookField bookField = field.userData as MathBookField;

            mathBook.inputOutputs.ReorderField(index, bookField);
        }

        void OnMenuItemTriggered(object userData)
        {
            var fieldType = (MathBookField.Direction)userData;
            var field = new MathBookField(fieldType);
            field.name = mathBook.inputOutputs.GenerateUniqueFieldName("New " + (fieldType == MathBookField.Direction.Input ? "input" : "output"));
            mathBook.inputOutputs.AddField(field);

            // Find newly added item to initiate rename.
            var item = this.Query<SimpleBlackboardField>().Where(f => f.field == field).First();

            item.OpenTextEditor();
        }

        public void Rebuild()
        {
            ClearFields();
            foreach (MathBookField input in mathBook.inputOutputs.inputs)
            {
                AddField(input);
            }

            foreach (MathBookField output in mathBook.inputOutputs.outputs)
            {
                AddField(output);
            }
        }
    }
}
#endif
