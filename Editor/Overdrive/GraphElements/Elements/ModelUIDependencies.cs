using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public static class ModelUIDependencies
    {
        static Dictionary<GUID, HashSet<IGraphElement>> s_ModelDependencies = new Dictionary<GUID, HashSet<IGraphElement>>();

        public static void AddDependency(this IGraphElementModel model, IGraphElement ui)
        {
            if (!s_ModelDependencies.TryGetValue(model.Guid, out var uiList))
            {
                uiList = new HashSet<IGraphElement>();
                s_ModelDependencies[model.Guid] = uiList;
            }

            uiList.Add(ui);
        }

        public static void RemoveDependency(this IGraphElementModel model, IGraphElement ui)
        {
            if (s_ModelDependencies.TryGetValue(model.Guid, out var uiList))
            {
                uiList.Remove(ui);
            }
        }

        public static IEnumerable<IGraphElement> GetDependencies(this IGraphElementModel model)
        {
            return s_ModelDependencies.TryGetValue(model.Guid, out var uiList) ? uiList : Enumerable.Empty<IGraphElement>();
        }
    }
}
