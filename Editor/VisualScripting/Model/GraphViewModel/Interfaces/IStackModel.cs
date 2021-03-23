using System;
using System.Collections.Generic;
using UnityEditor.VisualScripting.Model;
using UnityEngine;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    public interface IStackModel : INodeModel
    {
        IList<INodeModel> NodeModels { get; }
        IFunctionModel OwningFunctionModel { get; }
        bool AcceptNode(Type nodeType);
        bool DelegatesOutputsToNode(out INodeModel del);
        void CleanUp();
        IReadOnlyList<IPortModel> InputPorts { get; }
        IReadOnlyList<IPortModel> OutputPorts { get; }
        TNodeModel CreateStackedNode<TNodeModel>(string nodeName = "", int index = -1, SpawnFlags spawnFlags = SpawnFlags.Default, Action<TNodeModel> setup = null, GUID? guid = null) where TNodeModel : NodeModel;
        INodeModel CreateStackedNode(Type nodeTypeToCreate, string nodeName = "", int index = -1, SpawnFlags spawnFlags = SpawnFlags.Default, Action<NodeModel> preDefineSetup = null, GUID? guid = null);
    }
}
