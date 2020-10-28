using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [PublicAPI]
    public enum DragNDropContext
    {
        Blackboard, Graph
    }

    [PublicAPI]
    public interface IExternalDragNDropHandler
    {
        void HandleDragUpdated(DragUpdatedEvent e, DragNDropContext ctx);
        void HandleDragPerform(DragPerformEvent e, Store store, DragNDropContext ctx, VisualElement element);
    }
}
