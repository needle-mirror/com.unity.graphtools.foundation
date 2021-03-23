using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Vertical
{
    [Serializable]
    [SearcherItem(typeof(VerticalStencil), SearcherContext.Graph, "Vertical Node")]
    class VerticalNodeModel : NodeModel
    {
        [SerializeField, HideInInspector]
        int m_InputCount = 1;

        [SerializeField, HideInInspector]
        int m_OutputCount = 1;

        [SerializeField, HideInInspector]
        int m_VerticalInputCount = 1;

        [SerializeField, HideInInspector]
        int m_VerticalOutputCount = 1;

        public int InputCount => m_InputCount;

        public int OutputCount => m_OutputCount;

        public int VerticalInputCount => m_VerticalInputCount;

        public int VerticalOutputCount => m_VerticalOutputCount;

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            for (var i = 0; i < m_InputCount; i++)
                this.AddDataInputPort("In " + (i + 1), TypeHandle.Vector2, options: PortModelOptions.NoEmbeddedConstant);

            for (var i = 0; i < m_OutputCount; i++)
                this.AddDataOutputPort("Out " + (i + 1), TypeHandle.Vector2, options: PortModelOptions.NoEmbeddedConstant);

            for (var i = 0; i < m_VerticalInputCount; i++)
                this.AddExecutionInputPort("VIn " + (i + 1), orientation: Orientation.Vertical);

            for (var i = 0; i < m_VerticalOutputCount; i++)
                this.AddExecutionOutputPort("VOut " + (i + 1), orientation: Orientation.Vertical);
        }

        public void AddPort(Orientation orientation, Direction direction)
        {
            if (orientation == Orientation.Horizontal)
            {
                if (direction == Direction.Input)
                    m_InputCount++;
                else
                    m_OutputCount++;
            }
            else
            {
                if (direction == Direction.Input)
                    m_VerticalInputCount++;
                else
                    m_VerticalOutputCount++;
            }

            DefineNode();
        }

        public void RemovePort(Orientation orientation, Direction direction)
        {
            if (orientation == Orientation.Horizontal)
            {
                if (direction == Direction.Input)
                    m_InputCount--;
                else
                    m_OutputCount--;
            }
            else
            {
                if (direction == Direction.Input)
                    m_VerticalInputCount--;
                else
                    m_VerticalOutputCount--;
            }

            DefineNode();
        }
    }
}