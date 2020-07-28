using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public abstract class VSNodeModel : NodeModel
    {
        protected override PortModel CreatePort(Direction direction, string portName, PortType portType, TypeHandle dataType, string portId, PortModelOptions options)
        {
            return new VSPortModel(portName ?? "", portId, options)
            {
                Direction = direction,
                PortType = portType,
                DataTypeHandle = dataType,
                NodeModel = this
            };
        }
    }
}
