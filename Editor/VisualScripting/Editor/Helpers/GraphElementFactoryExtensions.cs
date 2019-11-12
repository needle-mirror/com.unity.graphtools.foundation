using System;
using JetBrains.Annotations;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    [GraphtoolsExtensionMethods]
    static class GraphElementFactoryExtensions
    {
        public static GraphElement CreateFunction(this INodeBuilder builder, Store store, FunctionModel model)
        {
            var functionNode = new FunctionNode(store, model, builder);
            return functionNode;
        }

        public static GraphElement CreateGetComponent(this INodeBuilder builder, Store store, HighLevelNodeModel model)
        {
            var functionNode = new HighLevelNode(model, store, builder.GraphView);
            return functionNode;
        }

        public static GraphElement CreateStack(this INodeBuilder builder, Store store, StackBaseModel model)
        {
            return new StackNode(store, model, builder);
        }

        public static GraphElement CreateIfConditionNode(this INodeBuilder builder, Store store, IfConditionNodeModel model)
        {
            return new IfConditionNode(model, store, builder.GraphView);
        }

        public static GraphElement CreateNode(this INodeBuilder builder, Store store, NodeModel model)
        {
            return new Node(model, store, builder.GraphView);
        }

        public static GraphElement CreateInlineExpressionNode(this INodeBuilder builder, Store store, InlineExpressionNodeModel model)
        {
            return new RenamableNode(model, store, builder.GraphView);
        }

        public static GraphElement CreateBinaryOperator(this INodeBuilder builder, Store store, BinaryOperatorNodeModel model)
        {
            return new Node(model, store, builder.GraphView)
            {
                CustomSearcherHandler = (node, nStore, pos, _) =>
                {
                    SearcherService.ShowEnumValues("Pick a new operator type", typeof(BinaryOperatorKind), pos, (pickedEnum, __) =>
                    {
                        if (pickedEnum != null)
                        {
                            ((BinaryOperatorNodeModel)node.model).kind = (BinaryOperatorKind)pickedEnum;
                            nStore.Dispatch(new RefreshUIAction(UpdateFlags.GraphTopology));
                        }
                    });
                    return true;
                }
            };
        }

        public static GraphElement CreateUnaryOperator(this INodeBuilder builder, Store store, UnaryOperatorNodeModel model)
        {
            return new Node(model, store, builder.GraphView)
            {
                CustomSearcherHandler = (node, nStore, pos, _) =>
                {
                    SearcherService.ShowEnumValues("Pick a new operator type", typeof(UnaryOperatorKind), pos, (pickedEnum, __) =>
                    {
                        if (pickedEnum != null)
                        {
                            ((UnaryOperatorNodeModel)node.model).kind = (UnaryOperatorKind)pickedEnum;
                            nStore.Dispatch(new RefreshUIAction(UpdateFlags.GraphTopology));
                        }
                    });
                    return true;
                }
            };
        }

        static void GetTokenPorts(Store store, INodeModel model, out Port inputPort, out Port outputPort)
        {
            inputPort = null;
            outputPort = null;

            // TODO: weirdly VariableNodeModels implement IHasMainOutputPort, but that 'output port' can be an input

            // Token only support one input port, we use the first one found.
            if (model is IHasMainInputPort inputModel)
            {
                var port = Port.Create(inputModel.InputPort, store, GetPortOrientation(inputModel.InputPort));
                SetupPort(port, ref inputPort, ref outputPort);
            }

            // Token only support one output port, we use the first one found.
            if (model is IHasMainOutputPort outputModel)
            {
                var port = Port.Create(outputModel.OutputPort, store, GetPortOrientation(outputModel.OutputPort));
                SetupPort(port, ref inputPort, ref outputPort);
            }

            void SetupPort(Port port, ref Port resultInputPort, ref Port resultOutputPort)
            {
                var className = port.direction == Direction.Input ? "left" : "right";
                port.AddToClassList(className);
                if (port.direction == Direction.Input)
                    resultInputPort = port;
                else
                    resultOutputPort = port;
            }

            Orientation GetPortOrientation(IPortModel port)
            {
                switch (port.PortType)
                {
                    case PortType.Data:
                    case PortType.Event:
                    case PortType.Instance:
                        return Orientation.Horizontal;
                    case PortType.Execution:
                        return Orientation.Vertical;
                    case PortType.Loop:
                        return port.Direction == Direction.Output ? Orientation.Vertical : Orientation.Horizontal;
                    default:
                        return Orientation.Horizontal;
                }
            }
        }

        public static GraphElement CreateToken(this INodeBuilder builder, Store store, IVariableModel model)
        {
            var isExposed = model.DeclarationModel?.IsExposed;
            Texture2D icon = (isExposed != null && isExposed.Value)
                ? VisualScriptingIconUtility.LoadIconRequired("GraphView/Nodes/BlackboardFieldExposed.png")
                : null;

            GetTokenPorts(store, model, out var input, out var output);

            var token = new Token(model, store, input, output, builder.GraphView, icon);
            if (model.DeclarationModel != null && model.DeclarationModel is LoopVariableDeclarationModel loopVariableDeclarationModel)
                VseUtility.AddTokenIcon(token, loopVariableDeclarationModel.TitleComponentIcon);
            return token;
        }

        public static GraphElement CreateConstantToken(this INodeBuilder builder, Store store, IConstantNodeModel model)
        {
            GetTokenPorts(store, model, out var input, out var output);

            return new Token(model, store, input, output, builder.GraphView);
        }

        public static GraphElement CreateToken(this INodeBuilder builder, Store store, IStringWrapperConstantModel model)
        {
            return CreateConstantToken(builder, store, model);
        }

        public static GraphElement CreateToken(this INodeBuilder builder, Store store, SystemConstantNodeModel model)
        {
            GetTokenPorts(store, model, out var input, out var output);

            return new Token(model, store, input, output, builder.GraphView);
        }

        public static GraphElement CreateStickyNote(this INodeBuilder builder, Store store, StickyNoteModel model)
        {
            return new StickyNote(store, model, model.Position, builder.GraphView);
        }

        public static GraphElement CreateMacro(this INodeBuilder builder, Store store, MacroRefNodeModel model)
        {
            return new MacroNode(model, store, builder.GraphView);
        }
    }
}
