using System;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Handles Drag and Drop of Blackboard Elements.
    /// Create a variable based on the Blackboard Field dragged
    /// </summary>
    public class BlackboardDragAndDropHandler : DragAndDropHandler
    {
        public Stencil Stencil => Dispatcher.GraphToolState.GraphModel.Stencil;
        protected ISelection Selection { get; }
        protected CommandDispatcher Dispatcher { get; }

        public BlackboardDragAndDropHandler(GraphView graphView)
            : this(graphView, graphView.CommandDispatcher)
        {
        }

        public BlackboardDragAndDropHandler(Blackboard blackboard)
            : this(blackboard, blackboard.CommandDispatcher)
        {
        }

        [PublicAPI]
        public BlackboardDragAndDropHandler(ISelection selection, CommandDispatcher dispatcher)
        {
            Selection = selection;
            Dispatcher = dispatcher;
        }

        public override void OnDragUpdated(DragUpdatedEvent e)
        {
            DragAndDrop.visualMode = e.ctrlKey ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Move;
        }

        public override void OnDragPerform(DragPerformEvent e)
        {
            var dragSelectionList = Selection.Selection.ToList();

            var dropElements = dragSelectionList.OfType<GraphElement>().ToList();

            var contentViewContainer = (e.target as GraphView)?.contentViewContainer ?? e.target as VisualElement;

            var variablesToCreate = dropElements
                .Select((e1, i) => (
                    Stencil.ExtractVariableFromGraphElement(e1),
                    (SerializableGUID)GUID.Generate(),
                    contentViewContainer.WorldToLocal(e.mousePosition) + i * GraphView.DragDropSpacer * Vector2.down))
                .ToList();

            var droppedNodes = dropElements.OfType<CollapsibleInOutNode>().ToList();

            if (droppedNodes.Any(e2 => !(e2.NodeModel is IVariableNodeModel)) && variablesToCreate.Any())
            {
                // no way to handle this ATM
                throw new ArgumentException("Unhandled case, dropping blackboard/variables fields and nodes at the same time");
            }

            if (variablesToCreate.Any())
                Stencil.OnDragAndDropVariableDeclarations(Dispatcher, variablesToCreate);
        }
    }
}
