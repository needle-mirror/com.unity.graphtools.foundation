using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class PortContainer : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<PortContainer> { }

        public static readonly string ussClassName = "ge-port-container";
        static readonly string portCountClassNamePrefix = ussClassName.WithUssModifier("port-count-");

        public PortContainer()
        {
            AddToClassList(ussClassName);
            this.AddStylesheet("PortContainer.uss");
        }

        public void UpdatePorts(IEnumerable<IPortModel> ports, GraphView graphView, CommandDispatcher commandDispatcher)
        {
            var uiPorts = this.Query<Port>().ToList();
            var portViewModels = ports?.ToList() ?? new List<IPortModel>();

            // Check if we should rebuild ports
            bool rebuildPorts = false;
            if (uiPorts.Count != portViewModels.Count)
            {
                rebuildPorts = true;
            }
            else
            {
                int i = 0;
                foreach (var portModel in portViewModels)
                {
                    if (!Equals(uiPorts[i].PortModel, portModel))
                    {
                        rebuildPorts = true;
                        break;
                    }

                    i++;
                }
            }

            if (rebuildPorts)
            {
                foreach (var visualElement in Children())
                {
                    if (visualElement is Port port)
                    {
                        port.RemoveFromGraphView();
                    }
                }

                Clear();

                foreach (var portModel in portViewModels)
                {
                    var ui = GraphElementFactory.CreateUI<Port>(graphView, commandDispatcher, portModel);
                    Debug.Assert(ui != null, "GraphElementFactory does not know how to create UI for " + portModel.GetType());
                    Add(ui);

                    ui.AddToGraphView(graphView);
                }
            }
            else
            {
                foreach (var port in uiPorts)
                {
                    port.UpdateFromModel();
                }
            }

            this.PrefixRemoveFromClassList(portCountClassNamePrefix);
            AddToClassList(portCountClassNamePrefix + portViewModels.Count);
        }
    }
}
