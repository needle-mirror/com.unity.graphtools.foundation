using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [Serializable]
    public class VSPortModel : PortModel
    {
        public override string ToolTip
        {
            get
            {
                string newTooltip = Direction == Direction.Output ? "Output" : "Input";
                if (PortType == PortType.Execution)
                {
                    newTooltip += " execution flow";
                }
                else if (PortType == PortType.Data)
                {
                    var stencil = GraphModel.Stencil;
                    newTooltip += $" of type {(DataTypeHandle == VSTypeHandle.ThisType ? (NodeModel?.GraphModel)?.FriendlyScriptName : DataTypeHandle.GetMetadata(stencil).FriendlyName)}";
                }

                return newTooltip;
            }
        }

        public VSPortModel(string name = null, string uniqueId = null, PortModelOptions options = PortModelOptions.Default)
            : base(name, uniqueId, options)
        {}
    }
}
