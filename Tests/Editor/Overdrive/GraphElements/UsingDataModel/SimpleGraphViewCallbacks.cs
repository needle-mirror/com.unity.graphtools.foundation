#if DISABLE_SIMPLE_MATH_TESTS
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Unity.GraphElements;
using UnityEngine.UIElements;

namespace Editor.UsingDataModel.NoPresenters
{
    internal struct SimpleGraphViewCallbacks
    {
        private MathBook m_MathBook;
        private SimpleGraphView m_GraphView;

        public void Init(MathBook mathBook, SimpleGraphView graphView)
        {
            m_MathBook = mathBook;
            m_GraphView = graphView;

            m_GraphView.graphViewChanged += GraphViewChanged;

            m_GraphView.elementsInsertedToStackNode = OnElementsInsertedToStackNode;
            m_GraphView.elementsRemovedFromStackNode = OnElementsRemovedFromStackNode;

            graphView.RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            graphView.RegisterCallback<DragPerformEvent>(OnDragPerformEvent);

            //m_GraphView.elementDeleted = ElementDeletedCallback;
            //m_GraphView.edgeConnected = EdgeConnected;
            //m_GraphView.edgeDisconnected = EdgeDisconnected;
        }

        private GraphViewChange GraphViewChanged(GraphViewChange graphViewChange)
        {
            bool needToComputeOutputs = false;

            if (graphViewChange.elementsToRemove != null)
            {
                foreach (GraphElement element in graphViewChange.elementsToRemove)
                {
                    if (element is Placemat || element is Node || element is BlackboardField || element is StickyNote)
                    {
                        ElementDeletedCallback(element);
                        if (element is BlackboardField)
                            m_GraphView.RebuildBlackboard();
                    }
                    else if (element is Edge)
                        EdgeDisconnected(element as Edge);
                }
                needToComputeOutputs = true;
            }

            if (graphViewChange.edgesToCreate != null)
            {
                foreach (Edge edge in graphViewChange.edgesToCreate)
                {
                    EdgeConnected(edge);
                }
                needToComputeOutputs = true;
            }

            if (graphViewChange.movedElements != null)
            {
                foreach (GraphElement element in graphViewChange.movedElements)
                {
                    MathNode mathNode = element.userData as MathNode;
                    if (mathNode == null)
                        continue;

                    mathNode.m_Position = element.layout.position;
                }
            }

            if (needToComputeOutputs)
            {
                m_MathBook.inputOutputs.ComputeOutputs();
            }

            return graphViewChange;
        }

        private void ElementDeletedCallback(VisualElement ve)
        {
            if (m_MathBook == null)
                return;

            switch (ve.userData)
            {
                case MathNode node:
                    m_GraphView.window.DestroyNode(node);
                    break;
                case MathPlacemat placemat:
                    m_GraphView.window.DestroyPlacemat(placemat);
                    break;
                case MathStickyNote stickyNote:
                    m_GraphView.window.DestroyStickyNote(stickyNote);
                    break;
                case MathBookField mathBookField:
                    m_MathBook.inputOutputs.RemoveField(mathBookField);

                    // Removes the containing row from its parent section
                    BlackboardRow row = ve.GetFirstAncestorOfType<BlackboardRow>();
                    if (row != null)
                        row.RemoveFromHierarchy();

                    break;
            }
        }

        private void OnElementsInsertedToStackNode(StackNode graphStackNode, int index, IEnumerable<GraphElement> elements)
        {
            var mathStackNode = graphStackNode.userData as MathStackNode;

            if (mathStackNode != null)
            {
                mathStackNode.InsertNodes(index, elements.Select(e => e.userData as MathNode));
            }
        }

        private void OnElementsRemovedFromStackNode(StackNode graphStackNode, IEnumerable<GraphElement> elements)
        {
            var mathStackNode = graphStackNode.userData as MathStackNode;

            if (mathStackNode != null)
            {
                mathStackNode.RemoveNodes(elements.Select(e => e.userData as MathNode));
            }
        }

        private void SetEdgeConnection(Edge edge, MathNode mathNode)
        {
            if (edge == null || edge.input == null || edge.output == null)
                return;

            if (edge.input.userData is MathOperator || edge.input.userData is MathFunction)
            {
                int inputIndex = 0;
                foreach (var input in edge.input.parent.Children())
                {
                    if (input == edge.input)
                        break;
                    inputIndex++;
                }

                var mathOperation = edge.input.userData as MathOperator;

                if (mathOperation)
                {
                    if (inputIndex == 0)
                        mathOperation.left = mathNode;
                    else
                        mathOperation.right = mathNode;
                }
                else
                {
                    var mathFunction = edge.input.userData as MathFunction;

                    mathFunction.SetParameter(inputIndex, mathNode);
                }
            }
            else if (edge.input.userData is MathResult)
            {
                var resultNode = edge.input.userData as MathResult;

                resultNode.root = mathNode;
            }

            if (mathNode != null)
                edge.viewDataKey = mathNode.nodeID + "_edge";
        }

        private void EdgeConnected(Edge edge)
        {
            if (edge == null || edge.output == null)
                return;

            var outputMathNode = edge.output.userData as MathNode;
            SetEdgeConnection(edge, outputMathNode);
        }

        private void EdgeDisconnected(Edge edge)
        {
            SetEdgeConnection(edge, null);
        }

        private void OnDragUpdatedEvent(DragUpdatedEvent e)
        {
            if (DragAndDrop.GetGenericData("DragSelection") is List<ISelectable> selection && (selection.OfType<BlackboardField>().Count() >= 0))
            {
                DragAndDrop.visualMode = e.actionKey ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Move;
            }
        }

        private void OnDragPerformEvent(DragPerformEvent e)
        {
            var selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;

            if (selection == null)
            {
                return;
            }

            IEnumerable<BlackboardField> fields = selection.OfType<BlackboardField>();

            if (!fields.Any())
                return;

            Vector2 localPos = (e.currentTarget as VisualElement).ChangeCoordinatesTo(m_GraphView.contentViewContainer, e.localMousePosition);

            foreach (BlackboardField field in fields)
            {
                MathBookField bookField = field.userData as MathBookField;

                if (bookField == null)
                    continue;

                MathNode fieldNode = null;

                if (bookField.direction == MathBookField.Direction.Input)
                {
                    var varFieldNode = ScriptableObject.CreateInstance<MathBookInputNode>();

                    varFieldNode.fieldName = bookField.name;
                    fieldNode = varFieldNode;
                }
                else
                {
                    var resFieldNode = ScriptableObject.CreateInstance<MathBookOutputNode>();

                    resFieldNode.fieldName = bookField.name;
                    fieldNode = resFieldNode;
                }

                fieldNode.m_Position = localPos;
                m_GraphView.window.AddNode(fieldNode);

                var visualNode = m_GraphView.window.CreateNode(fieldNode) as Node;

                m_GraphView.AddElement(visualNode);

                localPos += new Vector2(0, 25);
            }
        }

        public void DeInit(SimpleGraphView graphView)
        {
            // ReSharper disable once DelegateSubtraction
            graphView.graphViewChanged -= GraphViewChanged;
        }
    }
}
#endif
