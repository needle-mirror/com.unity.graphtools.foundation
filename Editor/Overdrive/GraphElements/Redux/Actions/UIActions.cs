using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Properties;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
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
