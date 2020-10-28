using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [GraphElementsExtensionMethodsCache]
    public static class GraphElementFactoryExtensions
    {
        public static IGraphElement CreatePort(this ElementBuilder elementBuilder, Store store, PortModel model)
        {
            var ui = new VisualScripting.Port();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView);
            return ui;
        }
    }
}
