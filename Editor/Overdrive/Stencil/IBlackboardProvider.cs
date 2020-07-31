using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IBlackboardProvider
    {
        IEnumerable<BlackboardSection> CreateSections();
        string GetSubTitle();
        void AddItemRequested<TAction>(VisualScripting.Store store, TAction action) where TAction : IAction;
        void MoveItemRequested(VisualScripting.Store store, int index, VisualElement field);
        void RebuildSections(Blackboard blackboard);
        void DisplayAppropriateSearcher(Vector2 mousePosition, Blackboard blackboard);
        bool CanAddItems { get; }
        void BuildContextualMenu(DropdownMenu evtMenu, VisualElement visualElement, VisualScripting.Store store, Vector2 mousePosition);
    }
}
