using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine.UIElements;
using Port = UnityEditor.Experimental.GraphView.Port;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    public interface IPortModel : IGraphElementModel
    {
        string Name { get; }
        string UniqueId { get; }
        INodeModel NodeModel { get; }
        ConstantNodeModel EmbeddedValue { get; }
        Action<IChangeEvent, Store, IPortModel> EmbeddedValueEditorValueChangedOverride { get; set; }
        bool CreateEmbeddedValueIfNeeded { get; }

        IEnumerable<IPortModel> ConnectionPortModels { get; }

        Direction Direction { get; }
        PortType PortType { get; }
        TypeHandle DataType { get; }
        Port.Capacity Capacity { get; }
        bool Connected { get; }
        Action OnValueChanged { get; set; }
        string ToolTip { get; }
    }
}
