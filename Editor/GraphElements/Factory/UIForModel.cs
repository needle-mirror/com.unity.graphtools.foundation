using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Utility to get the <see cref="IModelUI"/> that have been created for a <see cref="IGraphElementModel"/>.
    /// </summary>
    public static class UIForModel
    {
        static GraphElementMapping s_UIForModel = new GraphElementMapping();

        internal static void AddOrReplaceGraphElement(IModelUI modelUI)
        {
            s_UIForModel.AddOrReplaceUIForModel(modelUI);
        }

        [CanBeNull]
        public static T GetUI<T>(this IGraphElementModel model, IModelView view, string context = null) where T : class, IModelUI
        {
            return s_UIForModel.FirstOrDefault(view, context, model) as T;
        }

        [CanBeNull]
        internal static IModelUI GetUI(this IGraphElementModel model, IModelView view, string context = null)
        {
            return s_UIForModel.FirstOrDefault(view, context, model);
        }

        public static IEnumerable<IModelUI> GetAllUIs(this IGraphElementModel model, IModelView view)
        {
            return s_UIForModel.GetAllUIForModel(model).Where(ui => ui.View == view);
        }

        internal static void RemoveGraphElement(IModelUI modelUI)
        {
            s_UIForModel.RemoveGraphElement(modelUI);
        }

        internal static void Reset()
        {
            s_UIForModel = new GraphElementMapping();
        }
    }
}
