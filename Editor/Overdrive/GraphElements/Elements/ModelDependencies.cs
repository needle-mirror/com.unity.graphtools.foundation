using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public static class ModelDependencies
    {
        static Dictionary<SerializableGUID, HashSet<IModelUI>> s_ModelDependencies = new Dictionary<SerializableGUID, HashSet<IModelUI>>();

        public static void AddDependency(this IGraphElementModel model, IModelUI ui)
        {
            if (!s_ModelDependencies.TryGetValue(model.Guid, out var uiList))
            {
                uiList = new HashSet<IModelUI>();
                s_ModelDependencies[model.Guid] = uiList;
            }

            uiList.Add(ui);
        }

        public static void RemoveDependency(this IGraphElementModel model, IModelUI ui)
        {
            if (s_ModelDependencies.TryGetValue(model.Guid, out var uiList))
            {
                uiList.Remove(ui);
            }
        }

        public static IEnumerable<IModelUI> GetDependencies(this IGraphElementModel model)
        {
            return s_ModelDependencies.TryGetValue(model.Guid, out var uiList) ? uiList : Enumerable.Empty<IModelUI>();
        }
    }
}
