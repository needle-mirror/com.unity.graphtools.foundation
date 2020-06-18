using System;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public static class VSCreationExtensions
    {
        public static IVariableModel CreateVariableNode(this IGraphModel graphModel,
            IVariableDeclarationModel declarationModel, Vector2 position, SpawnFlags spawnFlags = SpawnFlags.Default, GUID? guid = null)
        {
            return graphModel.Stencil.CreateVariableModelForDeclaration(graphModel, declarationModel, position, spawnFlags, guid);
        }

        public static IConstantNodeModel CreateConstantNode(this IGraphModel graphModel, string constantName,
            TypeHandle constantTypeHandle, Vector2 position, SpawnFlags spawnFlags = SpawnFlags.Default, GUID? guid = null, Action<ConstantNodeModel> preDefine = null)
        {
            var nodeType = graphModel.Stencil.GetConstantNodeModelType(constantTypeHandle);

            void PreDefineSetup(NodeModel model)
            {
                if (model is ConstantNodeModel constantModel)
                {
                    constantModel.PredefineSetup(constantTypeHandle);
                    preDefine?.Invoke(constantModel);
                }
            }

            return (ConstantNodeModel)graphModel.CreateNode(nodeType, constantName, position, spawnFlags, PreDefineSetup, guid);
        }
    }

    public static class VSNodeDataCreationExtensions
    {
        public static INodeModel CreateNode(this IGraphNodeCreationData data, Type nodeType, string name = null, Action<NodeModel> preDefineSetup = null)
        {
            return data.GraphModel.CreateNode(nodeType, name, data.Position, data.SpawnFlags, preDefineSetup, data.Guid);
        }

        public static T CreateNode<T>(this IGraphNodeCreationData data, string name = null,  Action<T> preDefineSetup = null) where T : NodeModel
        {
            return data.GraphModel.CreateNode(name, data.Position, data.SpawnFlags, preDefineSetup, data.Guid);
        }

        public static IVariableModel CreateVariableNode(this IGraphNodeCreationData data, IVariableDeclarationModel declarationModel)
        {
            return data.GraphModel.CreateVariableNode(declarationModel, data.Position, data.SpawnFlags, data.Guid);
        }

        public static IConstantNodeModel CreateConstantNode(this IGraphNodeCreationData data, string constantName, TypeHandle typeHandle)
        {
            return data.GraphModel.CreateConstantNode(constantName, typeHandle, data.Position, data.SpawnFlags, data.Guid);
        }

        public static ISystemConstantNodeModel CreateSystemConstantNode(this IGraphNodeCreationData data, Type declaringType, Type constantType, string constantName)
        {
            void Setup(SystemConstantNodeModel n)
            {
                n.ReturnType = constantType.GenerateTypeHandle(data.GraphModel.Stencil);
                n.DeclaringType = declaringType.GenerateTypeHandle(data.GraphModel.Stencil);
                n.Identifier = constantName;
            }

            var name = $"{declaringType.FriendlyName(false)} > {constantName}";
            return data.GraphModel.CreateNode<SystemConstantNodeModel>(name, data.Position, data.SpawnFlags, Setup, data.Guid);
        }
    }
}
