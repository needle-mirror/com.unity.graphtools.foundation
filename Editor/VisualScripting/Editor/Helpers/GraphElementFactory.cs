using System;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    public interface INodeBuilder
    {
        GraphView GraphView { get; }
    }

    public class NodeBuilder : INodeBuilder
    {
        public GraphView GraphView { get; set; }

        public static Type KeySelector(MethodInfo x)
        {
            return x.GetParameters()[2].ParameterType;
        }

        public static bool FilterMethods(MethodInfo x)
        {
            if (x.ReturnType != typeof(GraphElement))
                return false;

            var parameters = x.GetParameters();
            return parameters.Length == 3 && parameters[1].ParameterType == typeof(Store);
        }
    }

    public static class GraphElementFactory
    {
        [CanBeNull]
        public static GraphElement CreateUI(GraphView graphView, Store store, IGraphElementModel model)
        {
            if (model == null)
            {
                Debug.LogError("GraphElementFactory could not create node because of a null reference model.");
                return null;
            }

            var ext = ModelUtility.ExtensionMethodCache<INodeBuilder>.GetExtensionMethod(
                model.GetType(),
                NodeBuilder.FilterMethods,
                NodeBuilder.KeySelector
            );

            GraphElement newElem = null;
            if (ext != null)
            {
                var nodeBuilder = new NodeBuilder { GraphView = graphView };
                newElem = (GraphElement)ext.Invoke(null, new object[] { nodeBuilder, store, model });
            }
            else if (model is INodeModel nodeModel)
                newElem = new Node(nodeModel, store, graphView);

            if (newElem == null)
                Debug.LogError($"GraphElementFactory doesn't know how to create a node of type: {model.GetType()}");
            else if (model is INodeModel nodeModel)
            {
                if (nodeModel.HasUserColor)
                    (newElem as ICustomColor)?.SetColor(nodeModel.Color);

                if (newElem is INodeState nodeState)
                {
                    if (nodeModel.State == ModelState.Disabled)
                        nodeState.UIState = NodeUIState.Disabled;
                    else
                        nodeState.UIState = NodeUIState.Enabled;
                    nodeState.ApplyNodeState();
                    nodeState.AddOverlay();
                }
            }

            return newElem;
        }
    }
}
