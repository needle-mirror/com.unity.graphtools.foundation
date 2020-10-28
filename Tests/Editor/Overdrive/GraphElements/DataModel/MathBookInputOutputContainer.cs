using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    public class MathBookInputOutputContainer : ScriptableObject
    {
        [SerializeField]
        private List<MathBookField> m_Inputs;

        [SerializeField]
        private List<MathBookField> m_Outputs;

        static private readonly List<MathBookField> s_EmptyFields = new List<MathBookField>();

        public List<MathBookField> inputs { get { return m_Inputs != null ? m_Inputs : s_EmptyFields; } }
        public List<MathBookField> outputs { get { return m_Outputs != null ? m_Outputs : s_EmptyFields; } }

        MathBook m_MathBook;

        public event Action<MathBookInputOutputContainer> changed;

        public MathBook mathBook
        {
            get
            {
                return m_MathBook;
            }
            set
            {
                if (m_MathBook == value)
                    return;

                m_MathBook = value;

                foreach (MathBookField input in m_Inputs)
                {
                    input.mathBook = mathBook;
                }

                foreach (MathBookField output in m_Outputs)
                {
                    output.mathBook = mathBook;
                }

                if (m_MathBook)
                {
                    ComputeOutputs();
                }
            }
        }

        public string GenerateUniqueFieldName(string fieldName)
        {
            List<string> allNames = inputs.Select(field => field.name).ToList();

            allNames.AddRange(outputs.Select(field => field.name).ToList());

            string tempName = string.Copy(fieldName);
            int tryCount = 0;

            while (allNames.Contains(tempName))
            {
                tempName = fieldName + " (" + ++tryCount + ")";
            }

            return tempName;
        }

        public MathBookField FindField(string name)
        {
            MathBookField field = inputs.Find(e => e.name == name);

            if (field == null)
            {
                field = outputs.Find(e => e.name == name);
            }

            return field;
        }

        public void ReorderField(int index, MathBookField field)
        {
            List<MathBookField> list = field.direction == MathBookField.Direction.Input ? m_Inputs : m_Outputs;

            if (index > list.IndexOf(field))
            {
                index--;
            }
            list.Remove(field);
            list.Insert(index, field);
            NotifyChange();
        }

        public void AddField(MathBookField field)
        {
            if (field == null)
                return;

            if (field.direction == MathBookField.Direction.Input)
            {
                if (m_Inputs == null)
                    m_Inputs = new List<MathBookField>();
                m_Inputs.Add(field);
            }
            else
            {
                if (m_Outputs == null)
                    m_Outputs = new List<MathBookField>();
                m_Outputs.Add(field);
            }

            field.mathBook = mathBook;
            NotifyChange();
        }

        public void RemoveField(MathBookField field)
        {
            if (field == null ||
                ((field.direction == MathBookField.Direction.Input) && m_Inputs == null) ||
                ((field.direction == MathBookField.Direction.Output) && m_Outputs == null))
                return;

            if (field.direction == MathBookField.Direction.Input)
            {
                if (m_Inputs == null)
                    return;
                m_Inputs.Remove(field);
            }
            else
            {
                if (m_Outputs == null)
                    return;
                m_Outputs.Remove(field);
            }

            field.mathBook = null;
            NotifyChange();
        }

        public void ClearInputs()
        {
            if (m_Inputs == null)
                return;

            m_Inputs.Clear();
            NotifyChange();
        }

        public void ClearOutputs()
        {
            if (m_Inputs == null)
                return;

            m_Inputs.Clear();
            NotifyChange();
        }

        public void OnEnable()
        {
            if (m_Inputs == null)
                m_Inputs = new List<MathBookField>();

            if (m_Outputs == null)
                m_Outputs = new List<MathBookField>();
        }

        public void NotifyFieldChange(MathBookField field, MathBookField.Change change)
        {
            if (mathBook == null)
                return;

            foreach (IMathBookFieldNode fieldNode in mathBook.nodes.OfType<IMathBookFieldNode>())
            {
                if (change.role == MathBookField.DataRole.Name)
                {
                    var oldName = (string)change.oldValue;

                    if (fieldNode.fieldName == oldName)
                        fieldNode.fieldName = (string)change.newValue;
                }
                if (fieldNode.fieldName == field.name)
                {
                    fieldNode.NotifyChange();
                }
            }

            if (change.role == MathBookField.DataRole.Value)
                ComputeOutputs();
        }

        public void ComputeOutputs()
        {
            if (mathBook == null)
                return;

            foreach (MathBookOutputNode outputNode in mathBook.nodes.OfType<MathBookOutputNode>())
            {
                MathBookField field = outputNode.field;

                if (field != null)
                {
                    field.value = outputNode.Evaluate();
                }
            }
        }

        public void NotifyChange()
        {
            if (changed != null)
            {
                changed(this);
            }
        }
    }
}
