using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public static class NodeDataCreationExtensions
    {
        public static INodeModel CreateNode(this IGraphNodeCreationData data, Type nodeType, string name = null, Action<INodeModel> preDefineSetup = null)
        {
            return data.GraphModel.CreateNode(nodeType, name, data.Position, data.SpawnFlags, preDefineSetup, data.Guid);
        }

        public static T CreateNode<T>(this IGraphNodeCreationData data, string name = null,  Action<T> preDefineSetup = null) where T : NodeModel
        {
            return data.GraphModel?.CreateNode(name, data.Position, data.SpawnFlags, preDefineSetup, data.Guid);
        }

        public static INodeModel CreateVariableNode(this IGraphNodeCreationData data, IVariableDeclarationModel declarationModel)
        {
            return data.GraphModel.CreateVariableNode(declarationModel, data.Position, data.SpawnFlags, data.Guid);
        }

        public static INodeModel CreateConstantNode(this IGraphNodeCreationData data, string constantName, TypeHandle typeHandle)
        {
            return data.GraphModel.CreateConstantNode(constantName, typeHandle, data.Position, data.SpawnFlags, data.Guid);
        }
    }
}
