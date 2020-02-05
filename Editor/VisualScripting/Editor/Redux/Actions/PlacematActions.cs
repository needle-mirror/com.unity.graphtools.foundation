#if UNITY_2020_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.EditorCommon.Redux;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.VisualScripting.Editor
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

    public class ChangePlacematTitleAction : IAction
    {
        public readonly PlacematModel[] PlacematModels;
        public readonly string Title;

        public ChangePlacematTitleAction(string title, params PlacematModel[] placematModels)
        {
            PlacematModels = placematModels;
            Title = title;
        }
    }

    public class ChangePlacematZOrdersAction : IAction
    {
        public readonly PlacematModel[] PlacematModels;
        public readonly int[] ZOrders;

        public ChangePlacematZOrdersAction(int[] zOrders, PlacematModel[] placematModels)
        {
            Assert.AreEqual(zOrders.Length, placematModels.Length, "You need to provide the same number of zOrder as placemats.");
#if UNITY_ASSERTIONS
            foreach (var zOrder in zOrders)
            {
                Assert.AreEqual(1, zOrders.Count(i => i == zOrder), "Each order should be unique in the provided list.");
            }
#endif
            PlacematModels = placematModels;
            ZOrders = zOrders;
        }
    }

    public class ChangePlacematPositionAction : IAction
    {
        public readonly PlacematModel[] PlacematModels;
        public readonly Rect Position;

        public ChangePlacematPositionAction(Rect position, params PlacematModel[] placematModels)
        {
            PlacematModels = placematModels;
            Position = position;
        }
    }

    public class ExpandOrCollapsePlacematAction : IAction
    {
        public readonly PlacematModel PlacematModel;
        public readonly bool Collapse;
        public readonly IEnumerable<string> CollapsedElements;

        public ExpandOrCollapsePlacematAction(bool collapse, IEnumerable<string> collapsedElements, PlacematModel placematModel)
        {
            PlacematModel = placematModel;
            Collapse = collapse;
            CollapsedElements = collapsedElements;
        }
    }
}
#endif
