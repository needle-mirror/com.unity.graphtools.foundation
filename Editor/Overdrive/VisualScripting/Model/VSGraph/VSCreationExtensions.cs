using System;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public static class VSNodeDataCreationExtensions
    {
        public static IGTFNodeModel CreateNode(this IGraphNodeCreationData data, Type nodeType, string name = null, Action<IGTFNodeModel> preDefineSetup = null)
        {
            return data.GraphModel.CreateNode(nodeType, name, data.Position, data.SpawnFlags, preDefineSetup, data.Guid);
        }

        public static T CreateNode<T>(this IGraphNodeCreationData data, string name = null,  Action<T> preDefineSetup = null) where T : NodeModel
        {
            return (data.GraphModel as IGraphModel)?.CreateNode(name, data.Position, data.SpawnFlags, preDefineSetup);
        }

        public static IGTFNodeModel CreateVariableNode(this IGraphNodeCreationData data, IGTFVariableDeclarationModel declarationModel)
        {
            return data.GraphModel.CreateVariableNode(declarationModel, data.Position, data.SpawnFlags, data.Guid);
        }

        public static IGTFNodeModel CreateConstantNode(this IGraphNodeCreationData data, string constantName, TypeHandle typeHandle)
        {
            return data.GraphModel.CreateConstantNode(constantName, typeHandle, data.Position, data.SpawnFlags, data.Guid);
        }
    }
}
