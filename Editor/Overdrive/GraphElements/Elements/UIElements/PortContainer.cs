using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class PortContainer : VisualElementBridge
    {
        public new class UxmlFactory : UxmlFactory<PortContainer> {}

        public PortContainer()
        {
            AddToClassList("ge-port-container");
            this.AddStylesheet("PortContainer.uss");
        }

        static readonly string sPortCountClassNamePrefix = "ge-port-container--port-count-";

        public void UpdatePorts(IEnumerable<IGTFPortModel> ports, GraphView graphView, Overdrive.Store store)
        {
            var uiPorts = this.Query<Port>().ToList();
            var portViewModels = ports?.ToList() ?? new List<IGTFPortModel>();

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
                Clear();
                foreach (var portModel in portViewModels)
                {
                    var ui = GraphElementFactory.CreateUI<Port>(graphView, store, portModel);
                    Debug.Assert(ui != null, "GraphElementFactory does not know how to create UI for " + portModel.GetType());
                    ui.Orientation = Orientation.Horizontal;
                    Add(ui);
                }
            }
            else
            {
                foreach (Port port in uiPorts)
                {
                    port.UpdateFromModel();
                }
            }

            this.PrefixRemoveFromClassList(sPortCountClassNamePrefix);
            AddToClassList(sPortCountClassNamePrefix + portViewModels.Count);
        }
    }
}
