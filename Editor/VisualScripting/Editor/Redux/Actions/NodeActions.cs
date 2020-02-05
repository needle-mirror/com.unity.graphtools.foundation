using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor.EditorCommon.Redux;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    public class CreateLogNodeAction : IAction
    {
        public readonly IStackModel StackModel;
        public readonly LogNodeModel.LogTypes LogType;

        public CreateLogNodeAction(IStackModel stackModel, LogNodeModel.LogTypes logType)
        {
            StackModel = stackModel;
            LogType = logType;
        }
    }

    public class DisconnectNodeAction : IAction
    {
        public readonly INodeModel[] NodeModels;

        public DisconnectNodeAction(params INodeModel[] nodeModels)
        {
            NodeModels = nodeModels;
        }
    }

    public class RemoveNodesAction : IAction
    {
        public readonly INodeModel[] ElementsToRemove;
        public readonly INodeModel[] NodesToBypass;

        public RemoveNodesAction(INodeModel[] nodesToBypass, INodeModel[] elementsToRemove)
        {
            ElementsToRemove = elementsToRemove;
            NodesToBypass = nodesToBypass;
        }
    }

    public class CreateNodeFromSearcherAction : IAction
    {
        public readonly IGraphModel GraphModel;
        public readonly Vector2 Position;
        public readonly GraphNodeModelSearcherItem SelectedItem;
        public readonly IReadOnlyList<GUID> Guids;

        public CreateNodeFromSearcherAction(IGraphModel graphModel, Vector2 position,
                                            GraphNodeModelSearcherItem selectedItem, IReadOnlyList<GUID> guids = null)
        {
            GraphModel = graphModel;
            Position = position;
            SelectedItem = selectedItem;
            Guids = guids;
        }
    }

    public class SetNodeEnabledStateAction : IAction
    {
        public readonly INodeModel[] NodeToConvert;
        public readonly ModelState State;

        public SetNodeEnabledStateAction(INodeModel[] nodeModel, ModelState state)
        {
            State = state;
            NodeToConvert = nodeModel;
        }
    }

    public class RefactorConvertToFunctionAction : IAction
    {
        public readonly INodeModel NodeToConvert;

        public RefactorConvertToFunctionAction(INodeModel nodeModel)
        {
            NodeToConvert = nodeModel;
        }
    }

    public class RefactorExtractMacroAction : IAction
    {
        public readonly List<IGraphElementModel> Selection;
        public readonly Vector2 Position;
        public readonly string MacroPath;

        public RefactorExtractMacroAction(List<IGraphElementModel> selection, Vector2 position, string macroPath)
        {
            Selection = selection;
            Position = position;
            MacroPath = macroPath;
        }
    }

    public class RefactorExtractFunctionAction : IAction
    {
        public readonly IList<ISelectable> Selection;

        public RefactorExtractFunctionAction(IList<ISelectable> selection)
        {
            Selection = selection;
        }
    }

    [PublicAPI]
    public class CreateMacroRefAction : IAction
    {
        public readonly GraphModel GraphModel;
        public readonly Vector2 Position;

        public CreateMacroRefAction(GraphModel graphModel, Vector2 position)
        {
            GraphModel = graphModel;
            Position = position;
        }
    }
}
