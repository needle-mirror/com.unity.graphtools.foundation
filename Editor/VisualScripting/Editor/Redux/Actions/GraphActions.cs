using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.EditorCommon.Redux;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    public class CreateFunctionAction : IAction
    {
        public readonly string Name;
        public readonly Vector2 Position;

        public CreateFunctionAction(string name, Vector2 position)
        {
            Name = name;
            Position = position;
        }
    }

    public class CreateEventFunctionAction : IAction
    {
        public readonly MethodInfo MethodInfo;
        public readonly Vector2 Position;

        public CreateEventFunctionAction(MethodInfo methodInfo, Vector2 position)
        {
            MethodInfo = methodInfo;
            Position = position;
        }
    }

    public class DeleteElementsAction : IAction
    {
        public readonly IReadOnlyCollection<IGraphElementModel> ElementsToRemove;

        public DeleteElementsAction(params IGraphElementModel[] elementsToRemove)
        {
            ElementsToRemove = elementsToRemove;
        }
    }

    public class RenameElementAction : IAction
    {
        public readonly IRenamableModel RenamableModel;
        public readonly string Name;

        public RenameElementAction(IRenamableModel renamableModel, string name)
        {
            RenamableModel = renamableModel;
            Name = name;
        }
    }

    public class MoveElementsAction : IAction
    {
        public readonly IReadOnlyCollection<NodeModel> NodeModels;
        public readonly IReadOnlyCollection<StickyNoteModel> StickyModels;
        public readonly Vector2 Delta;

        public MoveElementsAction(Vector2 delta,
                                  IReadOnlyCollection<NodeModel> nodeModels,
                                  IReadOnlyCollection<StickyNoteModel> stickyModels)
        {
            NodeModels = nodeModels;
            StickyModels = stickyModels;
            Delta = delta;
        }
    }

    public class PanToNodeAction : IAction
    {
        public readonly GUID nodeGuid;

        public PanToNodeAction(GUID nodeGuid)
        {
            this.nodeGuid = nodeGuid;
        }
    }
}
