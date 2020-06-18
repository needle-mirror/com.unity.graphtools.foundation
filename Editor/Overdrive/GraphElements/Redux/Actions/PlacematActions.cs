using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public abstract class GenericPlacematAction<DataType> : GenericModelAction<IGTFPlacematModel, DataType>
    {
        public GenericPlacematAction(IGTFPlacematModel[] models, DataType value) : base(models, value)
        {
        }
    }

    public class ChangePlacematColorAction : GenericPlacematAction<Color>
    {
        public ChangePlacematColorAction(Color color, IGTFPlacematModel placematModel)
            : base(new[] { placematModel }, color) {}

        public static TState DefaultReducer<TState>(TState previousState, ChangePlacematColorAction action) where TState : State
        {
            foreach (var model in action.Models)
                model.Color = action.Value;

            return previousState;
        }
    }

    public class ChangePlacematZOrdersAction : GenericPlacematAction<int[]>
    {
        public ChangePlacematZOrdersAction(int[] zOrders, IGTFPlacematModel[] placematModels)
            : base(placematModels, zOrders)
        {
            Assert.AreEqual(zOrders.Length, placematModels.Length, "You need to provide the same number of zOrder as placemats.");
            foreach (var zOrder in zOrders)
            {
                Assert.AreEqual(1, zOrders.Count(i => i == zOrder), "Each order should be unique in the provided list.");
            }
        }

        public static TState DefaultReducer<TState>(TState previousState, ChangePlacematZOrdersAction action) where TState : State
        {
            for (var i = 0; i < action.Models.Length; i++)
            {
                action.Models[i].ZOrder = action.Value[i];
            }

            return previousState;
        }
    }

    public class ChangePlacematPositionAction : GenericPlacematAction<Rect>
    {
        public ResizeFlags ResizeFlags;

        public ChangePlacematPositionAction(Rect position, ResizeFlags resizeWhat, params IGTFPlacematModel[] placematModels)
            : base(placematModels, position)
        {
            ResizeFlags = resizeWhat;
        }

        public static TState DefaultReducer<TState>(TState previousState, ChangePlacematPositionAction action) where TState : State
        {
            foreach (var model in action.Models)
            {
                var newRect = model.PositionAndSize;
                if ((action.ResizeFlags & ResizeFlags.Left) == ResizeFlags.Left)
                {
                    newRect.x = action.Value.x;
                }
                if ((action.ResizeFlags & ResizeFlags.Top) == ResizeFlags.Top)
                {
                    newRect.y = action.Value.y;
                }
                if ((action.ResizeFlags & ResizeFlags.Width) == ResizeFlags.Width)
                {
                    newRect.width = action.Value.width;
                }
                if ((action.ResizeFlags & ResizeFlags.Height) == ResizeFlags.Height)
                {
                    newRect.height = action.Value.height;
                }
                model.PositionAndSize = newRect;
            }

            return previousState;
        }
    }

    public class ExpandOrCollapsePlacematAction : IAction
    {
        public readonly IGTFPlacematModel PlacematModel;
        public readonly bool Collapse;
        public readonly IEnumerable<IGTFGraphElementModel> CollapsedElements;

        public ExpandOrCollapsePlacematAction(bool collapse, IEnumerable<IGTFGraphElementModel> collapsedElements, IGTFPlacematModel placematModel)
        {
            PlacematModel = placematModel;
            Collapse = collapse;
            CollapsedElements = collapsedElements;
        }

        public static TState DefaultReducer<TState>(TState previousState, ExpandOrCollapsePlacematAction action) where TState : State
        {
            action.PlacematModel.Collapsed = action.Collapse;
            action.PlacematModel.HiddenElements = action.PlacematModel.Collapsed ? action.CollapsedElements : null;

            return previousState;
        }
    }
}
