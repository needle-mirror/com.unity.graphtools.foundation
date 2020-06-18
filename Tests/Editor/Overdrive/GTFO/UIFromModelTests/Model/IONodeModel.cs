using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    class IONodeModel : IGTFNodeModel, IHasIOPorts, IHasTitle, ICollapsible
    {
        public IGTFGraphModel GraphModel { get; set; }
        public Vector2 Position { get; set; }

        public void Move(Vector2 delta)
        {
            Position += delta;
        }

        public bool IsDeletable => true;
        public bool IsDroppable => true;
        public bool IsCopiable => true;
        public string Title { get; set; }
        public string DisplayTitle => Title;

        public void CreatePorts(int inputPorts, int outputPorts)
        {
            m_InputPorts.Clear();
            for (var i = 0; i < inputPorts; i++)
            {
                m_InputPorts.Add(new PortModel
                {
                    Direction = Direction.Input,
                    NodeModel = this,
                    GraphModel = GraphModel
                });
            }

            m_OutputPorts.Clear();
            for (var i = 0; i < outputPorts; i++)
            {
                m_OutputPorts.Add(new PortModel
                {
                    Direction = Direction.Output,
                    NodeModel = this,
                    GraphModel = GraphModel
                });
            }
        }

        List<PortModel> m_InputPorts = new List<PortModel>();
        List<PortModel> m_OutputPorts = new List<PortModel>();
        public IEnumerable<IGTFPortModel> InputPorts => m_InputPorts;
        public IEnumerable<IGTFPortModel> OutputPorts => m_OutputPorts;
        public IEnumerable<IGTFPortModel> Ports => InputPorts.Concat(OutputPorts);
        public bool Collapsed { get; set; }
    }
}
