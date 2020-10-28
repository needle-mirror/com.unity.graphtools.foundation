using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IBlackboardProvider
    {
        IEnumerable<BlackboardSection> CreateSections();
        string GetSubTitle();
        void AddItemRequested<TAction>(Store store, TAction action) where TAction : BaseAction;
        void MoveItemRequested(Store store, int index, VisualElement field);
        void RebuildSections(Blackboard blackboard);
        void DisplayAppropriateSearcher(Vector2 mousePosition, Blackboard blackboard);
        bool CanAddItems { get; }
    }
}
