using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public abstract class GenericModelAction<ModelType, DataType> : IAction
    {
        public ModelType[] Models;
        public DataType Value;

        public GenericModelAction(ModelType[] models, DataType value)
        {
            Models = models;
            Value = value;
        }
    }

    public abstract class GenericNodeAction<DataType> : GenericModelAction<IGTFNodeModel, DataType>
    {
        public GenericNodeAction(IGTFNodeModel[] nodeModels, DataType value)
            : base(nodeModels, value) {}
    }

    public class SetNodePositionAction : GenericNodeAction<Vector2>
    {
        public SetNodePositionAction(IGTFNodeModel[] nodeModels, Vector2 value)
            : base(nodeModels, value) {}

        public static TState DefaultReducer<TState>(TState previousState, SetNodePositionAction action) where TState : State
        {
            foreach (var model in action.Models)
                model.Position = action.Value;

            return previousState;
        }
    }

    public class SetNodeCollapsedAction : GenericNodeAction<bool>
    {
        public SetNodeCollapsedAction(IGTFNodeModel model, bool value)
            : base(new[] { model }, value) {}

        public static TState DefaultReducer<TState>(TState previousState, SetNodeCollapsedAction action) where TState : State
        {
            foreach (var model in action.Models.OfType<ICollapsible>())
                model.Collapsed = action.Value;

            return previousState;
        }
    }

    public class DropEdgeInEmptyRegionAction : IAction
    {
        public readonly IGTFPortModel PortController;
        public readonly Vector2 Position;
        public readonly IEnumerable<IGTFEdgeModel> EdgesToDelete;

        public DropEdgeInEmptyRegionAction(IGTFPortModel portController, Vector2 position, IEnumerable<IGTFEdgeModel> edgesToDelete)
        {
            PortController = portController;
            Position = position;
            EdgesToDelete = edgesToDelete;
        }

        public static TState DefaultReducer<TState>(TState previousState, DropEdgeInEmptyRegionAction action) where TState : State
        {
            previousState.GraphModel.DeleteElements(action.EdgesToDelete);
            return previousState;
        }
    }

    public class RenameElementAction : IAction
    {
        public readonly IRenamable RenamableModel;
        public readonly string Name;

        public RenameElementAction(IRenamable renamableModel, string name)
        {
            RenamableModel = renamableModel;
            Name = name;
        }

        public static TState DefaultReducer<TState>(TState previousState, RenameElementAction action) where TState : State
        {
            action.RenamableModel.Rename(action.Name);
            return previousState;
        }
    }
}
