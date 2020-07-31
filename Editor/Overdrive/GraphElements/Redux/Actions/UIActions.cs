using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Properties;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    // PF FIXME: not a real action.
    public class RefreshUIAction : IAction
    {
        public UpdateFlags UpdateFlags;
        public List<IGTFGraphElementModel> ChangedModels;

        public RefreshUIAction()
        {
        }

        public RefreshUIAction(UpdateFlags updateFlags, List<IGTFGraphElementModel> changedModels = null)
        {
            UpdateFlags = updateFlags;
            ChangedModels = changedModels;
        }

        public RefreshUIAction(List<IGTFGraphElementModel> changedModels) : this(UpdateFlags.None, changedModels)
        {
        }
    }

    // PF FIXME: not a real action.
    [PublicAPI]
    public class OpenDocumentationAction : IAction
    {
        public readonly IGTFNodeModel[] NodeModels;

        public OpenDocumentationAction(params IGTFNodeModel[] nodeModels)
        {
            NodeModels = nodeModels;
        }
    }
}
