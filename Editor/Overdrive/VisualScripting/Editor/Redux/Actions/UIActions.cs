using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class RefreshUIAction : IAction
    {
        public readonly UpdateFlags UpdateFlags;
        public readonly List<IGraphElementModel> ChangedModels;

        public RefreshUIAction(UpdateFlags updateFlags, List<IGraphElementModel> changedModels = null)
        {
            UpdateFlags = updateFlags;
            ChangedModels = changedModels;
        }

        public RefreshUIAction(List<IGraphElementModel> changedModels) : this(UpdateFlags.None, changedModels)
        {
        }
    }

    [PublicAPI]
    public class OpenDocumentationAction : IAction
    {
        public readonly INodeModel[] NodeModels;

        public OpenDocumentationAction(params INodeModel[] nodeModels)
        {
            NodeModels = nodeModels;
        }
    }
}
