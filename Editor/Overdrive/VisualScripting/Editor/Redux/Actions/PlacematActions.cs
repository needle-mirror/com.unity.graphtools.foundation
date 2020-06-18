using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class CreatePlacematAction : IAction
    {
        public string Title;
        public Rect Position;

        public CreatePlacematAction(string title, Rect position)
        {
            Title = title;
            Position = position;
        }
    }
}
