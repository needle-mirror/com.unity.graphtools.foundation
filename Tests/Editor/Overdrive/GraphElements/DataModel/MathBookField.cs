using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    [Serializable]
    public class MathBookField
    {
        public enum DataRole
        {
            Exposed,
            Name,
            Owner,
            Tooltip,
            Value
        }

        public enum Direction
        {
            Input,
            Output,
        }

        public struct Change
        {
            public DataRole role;
            public object newValue;
            public object oldValue;
        }

        [SerializeField]
        private Direction m_Direction;

        [SerializeField]
        private string m_Name;

        [SerializeField]
        private bool m_Exposed;

        [SerializeField]
        private string m_ToolTip;

        [SerializeField]
        private float m_Value;

        private MathBook m_MathBook;

        public MathBook mathBook
        {
            get { return m_MathBook; }
            set
            {
                if (m_MathBook == value)
                    return;

                MathBook oldValue = m_MathBook;

                m_MathBook = value;

                NotifyChange(DataRole.Owner, value, oldValue);
            }
        }

        public Direction direction
        {
            get { return m_Direction; }
        }

        public string name
        {
            get { return m_Name; }
            set
            {
                if (m_Name == value)
                    return;

                string oldValue = m_Name;

                m_Name = value;

                NotifyChange(DataRole.Name, value, oldValue);
            }
        }

        public bool exposed
        {
            get { return m_Exposed; }
            set
            {
                if (m_Exposed == value)
                    return;

                bool oldValue = m_Exposed;

                m_Exposed = value;

                NotifyChange(DataRole.Exposed, value, oldValue);
            }
        }

        public string toolTip
        {
            get { return m_ToolTip; }
            set
            {
                if (m_ToolTip == value)
                    return;

                string oldValue = m_ToolTip;

                m_ToolTip = value;

                NotifyChange(DataRole.Exposed, value, oldValue);
            }
        }

        public float value
        {
            get { return m_Value; }
            set
            {
                if (m_Value == value)
                    return;

                float oldValue = m_Value;

                m_Value = value;

                NotifyChange(DataRole.Value, value, oldValue);
            }
        }

        public event Action<MathBookField, Change> changed;

        public MathBookField(Direction type)
        {
            m_Direction = type;
            m_Exposed = false;
            m_Value = 0.0f;
        }

        private void NotifyChange(DataRole role, object newValue, object oldValue)
        {
            var change = new Change { role = role, newValue = newValue, oldValue = oldValue };

            if (changed != null)
                changed(this, change);

            MathBook book = mathBook;

            // If the field is being removed from a book then send the notification through the former book
            if (role == DataRole.Owner)
            {
                if (oldValue != null)
                {
                    book = oldValue as MathBook;
                }
            }

            if (book != null)
                book.inputOutputs.NotifyFieldChange(this, change);
        }
    }
}
