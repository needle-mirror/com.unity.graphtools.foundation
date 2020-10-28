using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    public class MathBookInputNode : MathValueNode, IMathBookFieldNode
    {
        [SerializeField]
        private string m_FieldName;

        public event Action<IMathBookFieldNode> changed;

        public string fieldName
        {
            get
            {
                return m_FieldName;
            }
            set
            {
                m_FieldName = value;
            }
        }

        public MathBookField.Direction direction { get { return MathBookField.Direction.Input; } }

        public MathBookField field
        {
            get
            {
                if (mathBook != null)
                {
                    return mathBook.inputOutputs.FindField(m_FieldName);
                }
                return null;
            }
        }

        public override float value
        {
            get
            {
                if (field != null)
                {
                    return field.value;
                }

                return 0;
            }
        }

        public void OnEnable()
        {
            name = "MathBookInputNodeNode";
        }

        public override void ResetConnections()
        {
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
