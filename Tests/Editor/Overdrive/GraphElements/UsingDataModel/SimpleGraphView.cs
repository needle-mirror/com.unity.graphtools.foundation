#if DISABLE_SIMPLE_MATH_TESTS
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.UsingDataModel.NoPresenters
{
    class SimpleGraphView : GraphView
    {
        private static readonly Vector2 s_CopyOffset = new Vector2(50, 50);
        SimpleGraphViewWindow m_SimpleGraphViewWindow;
        SimpleBlackboard m_Blackboard;

        public SimpleGraphViewWindow window
        {
            get { return m_SimpleGraphViewWindow; }
        }

        public override bool supportsWindowedBlackboard
        {
            get { return true; }
        }

        class HackedSelectionDragger : SelectionDragger
        {
            public bool IsActive => m_Active;
        }

        HackedSelectionDragger m_HackedSelectionDragger;

        public bool IsSelectionDraggerActive => m_HackedSelectionDragger.IsActive;

        public SimpleGraphView(SimpleGraphViewWindow simpleGraphViewWindow, bool withWindowedTools)
        {
            m_SimpleGraphViewWindow = simpleGraphViewWindow;

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            // FIXME: add a coordinator so that ContentDragger and SelectionDragger cannot be active at the same time.
            this.AddManipulator(new ContentDragger());
            m_HackedSelectionDragger = new HackedSelectionDragger();
            this.AddManipulator(m_HackedSelectionDragger);
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new FreehandSelector());

            Insert(0, new GridBackground());

            focusable = true;

            serializeGraphElements = SerializeGraphElementsImplementation;
            canPasteSerializedData = CanPasteSerializedDataImplementation;
            unserializeAndPaste = UnserializeAndPasteImplementation;

            m_Blackboard = new SimpleBlackboard(simpleGraphViewWindow.mathBook, this);
            m_Blackboard.AddStylesheet("SimpleGraph");

            if (!withWindowedTools)
            {
                Add(m_Blackboard);
            }

            m_SimpleGraphViewWindow.mathBook.inputOutputs.changed += OnInputOutputsChanged;

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            UnregisterCallback<KeyDownEvent>(OnKeyDownShortcut);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            RegisterCallback<KeyDownEvent>(OnKeyDownShortcut);
        }

        void OnKeyDownShortcut(KeyDownEvent evt)
        {
            if (panel.GetCapturingElement(PointerId.mousePointerId) != null)
                return;

            EventPropagation result = EventPropagation.Continue;
            switch (evt.keyCode)
            {
                case KeyCode.G:
                    if (evt.actionKey)
                    {
                        AddToPlacemat();
                        result = EventPropagation.Continue;
                    }

                    break;
            }

            if (result == EventPropagation.Stop)
            {
                evt.StopPropagation();
                evt.imguiEvent?.Use();
            }
        }

        void OnInputOutputsChanged(MathBookInputOutputContainer obj)
        {
            RebuildBlackboard();
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target == this || selection.FindAll(s => s is VisualElement ve && ve.userData is MathNode).Contains(evt.target as ISelectable))
            {
                evt.menu.AppendAction("Group Selection (Placemats)", AddToPlacematAction, a =>
                {
                    List<ISelectable> filteredSelection = new List<ISelectable>();
                    foreach (ISelectable selectedObject in selection)
                    {
                        VisualElement ve = selectedObject as VisualElement;
                        if (ve?.userData is MathNode)
                        {
                            filteredSelection.Add(selectedObject);
                        }
                    }

                    return filteredSelection.Count > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
                });

                evt.menu.AppendSeparator();
            }

            if (evt.target == this)
            {
                evt.menu.AppendAction("Create Stack", CreateStackNode, DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendAction("Create Renamable Stack", CreateRenamableStackNode, DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendAction("Create Placemat", CreatePlacemat, DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendAction("Create StickyNote", CreateStickyNote, DropdownMenuAction.AlwaysEnabled);
            }

            base.BuildContextualMenu(evt);

            evt.menu.AppendAction("Rename Graph View", a =>
            {
                name = name.First() == 'A' ? "A" + name : "A " + name;
                m_SimpleGraphViewWindow.titleContent.text = name;
            }, DropdownMenuAction.AlwaysEnabled);
        }

        const string m_SerializedDataMimeType = "application/vnd.unity.simplegraphview.elements";

        static string SerializeGraphElementsImplementation(IEnumerable<GraphElement> elements)
        {
            List<ScriptableObject> data = new List<ScriptableObject>();
            foreach (var element in elements)
            {
                if (element.userData is MathNode mathNode)
                    data.Add(mathNode);
                else if (element.userData is MathPlacemat mathPlacemat)
                    data.Add(mathPlacemat);
            }

            CopyPasteData<ScriptableObject> copyPasteData = new CopyPasteData<ScriptableObject>(data);
            return m_SerializedDataMimeType + " " + JsonUtility.ToJson(copyPasteData);
        }

        static bool CanPasteSerializedDataImplementation(string data)
        {
            return data.StartsWith(m_SerializedDataMimeType);
        }

        void UnserializeAndPasteImplementation(string operationName, string serializedData)
        {
            CopyPasteData<ScriptableObject> data = JsonUtility.FromJson<CopyPasteData<ScriptableObject>>(serializedData.Substring(m_SerializedDataMimeType.Length + 1));

            if (data != null)
            {
                // Come up with top left most element's position to find the delta we're going to compute from the mouse position
                var placemats = data.GetPlacemats(); //.Select(e => e.position.position);
                var topLeftPosition = Vector2.positiveInfinity;
                var delta = s_CopyOffset;
                foreach (var placemat in placemats)
                {
                    var position = placemat.position;
                    if (position.x < topLeftPosition.x)
                    {
                        topLeftPosition = position.position;
                        delta = s_CopyOffset + position.size;
                    }
                }

                var ids = new Dictionary<string, string>();

                if (data.GetNodes().Any())
                {
                    foreach (MathNode mathNode in data.GetNodes())
                    {
                        string oldID = mathNode.nodeID.ToString();

                        ids[oldID] = mathNode.RewriteID().ToString();

                        m_SimpleGraphViewWindow.AddNode(mathNode);
                        mathNode.m_Position += delta;
                    }

                    // Remap ids
                    foreach (MathNode mathNode in data.GetNodes())
                    {
                        mathNode.RemapReferences(ids);
                    }
                }

                if (data.GetPlacemats().Any())
                {
                    // ZOrder is 1 based.
                    var nextZ = m_SimpleGraphViewWindow.mathBook.placemats.Count + 1;
                    foreach (MathPlacemat mathPlacemat in data.GetPlacemats().OrderBy(e => e.zOrder))
                    {
                        string oldID = mathPlacemat.identification;

                        ids[oldID] = mathPlacemat.RewriteID();

                        mathPlacemat.position = new Rect(mathPlacemat.position.x + delta.x,
                            mathPlacemat.position.y + delta.y,
                            mathPlacemat.position.width,
                            mathPlacemat.position.height);

                        // Put new placemats on top.
                        mathPlacemat.zOrder = nextZ++;
                    }

                    foreach (MathPlacemat mathPlacemat in data.GetPlacemats())
                    {
                        mathPlacemat.RemapReferences(ids);
                        m_SimpleGraphViewWindow.AddPlacemat(mathPlacemat);
                    }
                }

                Reload(data.GetNodes(), data.GetPlacemats(), null, ids);
            }
        }

        private void CreateStickyNote(DropdownMenuAction a)
        {
            Vector2 pos = a.eventInfo.localMousePosition;

            var stickyNote = ScriptableObject.CreateInstance<MathStickyNote>();
            Vector2 localPos = VisualElementExtensions.ChangeCoordinatesTo(this, contentViewContainer, pos);

            stickyNote.title = "New Title";
            stickyNote.contents = "Type something here";
            stickyNote.position = new Rect(localPos, new Vector2(200, 180));

            m_SimpleGraphViewWindow.AddStickyNote(stickyNote);

            var simpleStickyNote = new SimpleStickyNote();
            simpleStickyNote.model = stickyNote;
            simpleStickyNote.userData = stickyNote;

            AddElement(simpleStickyNote);
        }

        void AddToPlacematAction(DropdownMenuAction a)
        {
            AddToPlacemat();
        }

        void AddToPlacemat()
        {
            var pos = new Rect();
            var nodeList = new List<GraphElement>();

            foreach (ISelectable s in selection)
            {
                if (s is Node n)
                    nodeList.Add(n);
            }

            if (nodeList.Count > 0 && Placemat.ComputeElementBounds(ref pos, nodeList))
                CreatePlacemat(pos);
        }

        private void CreateRenamableStackNode(DropdownMenuAction a)
        {
            var graphStackNode = CreateGraphMathStackNode(a.eventInfo.localMousePosition);
            graphStackNode.capabilities |= Capabilities.Renamable;

            AddElement(graphStackNode);
        }

        private void CreateStackNode(DropdownMenuAction a)
        {
            var graphStackNode = CreateGraphMathStackNode(a.eventInfo.localMousePosition);

            AddElement(graphStackNode);
        }

        private GraphElement CreateGraphMathStackNode(Vector2 pos)
        {
            var stackNode = ScriptableObject.CreateInstance<MathStackNode>();
            Vector2 localPos = VisualElementExtensions.ChangeCoordinatesTo(this, contentViewContainer, pos);

            stackNode.name = "Stack";
            stackNode.m_Position = localPos;

            m_SimpleGraphViewWindow.AddNode(stackNode);

            return m_SimpleGraphViewWindow.CreateNode(stackNode);
        }

        void CreatePlacemat(DropdownMenuAction a)
        {
            Vector2 pos = a.eventInfo.localMousePosition;
            Vector2 localPos = VisualElementExtensions.ChangeCoordinatesTo(this, contentViewContainer, pos);

            CreatePlacemat(new Rect(localPos.x, localPos.y, 200, 200));
        }

        void CreatePlacemat(Rect pos)
        {
            // Create the model
            int zOrder = placematContainer.GetTopZOrder();
            var placematModel = MathPlacemat.CreateInstance(pos, zOrder);
            window.AddPlacemat(placematModel);

            // Create the UI
            var placemat = placematContainer.CreatePlacemat<SimplePlacemat>(placematModel.position, placematModel.zOrder, placematModel.title);
            placemat.userData = placematModel;
            placemat.viewDataKey = placematModel.identification;
            placemat.Model = placematModel;

            placemat.StartEditTitle();
        }

        private void AddToStackNode(DropdownMenuAction a)
        {
            var stackNode = ScriptableObject.CreateInstance<MathStackNode>();

            m_SimpleGraphViewWindow.AddNode(stackNode);

            var graphStackNode = m_SimpleGraphViewWindow.CreateNode(stackNode) as StackNode;

            AddElement(graphStackNode);

            ISelectable[] selectedElement = selection.ToArray();

            foreach (ISelectable s in selectedElement)
            {
                var node = s as SimpleNode;

                // Do not add edges
                if (node == null)
                    continue;

                graphStackNode.AddElement(node);

                node.Select(this, true);
            }
        }

        public void Reload(IEnumerable<MathNode> nodesToReload, IEnumerable<MathPlacemat> placemats, IEnumerable<MathStickyNote> stickies, Dictionary<string, string> oldToNewIdMapping = null)
        {
            string oldId;
            var nodes = new Dictionary<MathNode, GraphElement>();
            var oldIdToNewNode = new Dictionary<string, ISelectable>();

            var newToOldIdMapping = new Dictionary<string, string>();
            if (oldToNewIdMapping != null)
            {
                foreach (var oldIdKV in oldToNewIdMapping)
                {
                    newToOldIdMapping[oldIdKV.Value] = oldIdKV.Key;
                }
            }

            // Create the nodes.
            foreach (MathNode mathNode in nodesToReload)
            {
                GraphElement node = m_SimpleGraphViewWindow.CreateNode(mathNode);
                if (node == null)
                {
                    Debug.LogError("Could not create node " + mathNode);
                    continue;
                }

                node.name = "SimpleNode";
                nodes[mathNode] = node;

                AddElement(node);
            }

            // Add to stacks
            foreach (MathNode mathNode in nodesToReload)
            {
                MathStackNode stack = mathNode as MathStackNode;

                if (stack == null)
                    continue;

                StackNode graphStackNode = nodes[stack] as StackNode;

                for (int i = 0; i < stack.nodeCount; ++i)
                {
                    MathNode stackMember = stack.GetNode(i);
                    if (stackMember == null)
                    {
                        Debug.LogWarning("null stack member! Item " + i + " of stack " + stack.name + " is null. Possibly a leftover from bad previous manips.");
                    }

                    graphStackNode.AddElement(nodes[stackMember]);
                }
            }

            // Connect the presenters.
            foreach (var mathNode in nodesToReload)
            {
                if (mathNode is MathOperator)
                {
                    MathOperator mathOperator = mathNode as MathOperator;

                    if (!nodes.ContainsKey(mathNode))
                    {
                        Debug.LogError("No element found for " + mathNode);
                        continue;
                    }

                    var graphNode = nodes[mathNode] as Node;

                    if (mathOperator.left != null && nodes.ContainsKey(mathOperator.left))
                    {
                        var outputPort = (nodes[mathOperator.left] as Node).outputContainer[0] as Port;
                        var inputPort = graphNode.inputContainer[0] as Port;

                        Edge edge = inputPort.ConnectTo(outputPort);
                        edge.viewDataKey = mathOperator.left.nodeID + "_edge";
                        AddElement(edge);
                    }
                    else if (mathOperator.left != null)
                    {
                        //add.m_Left = null;
                        Debug.LogWarning("Invalid left operand for operator " + mathOperator + " , " + mathOperator.left);
                    }

                    if (mathOperator.right != null && nodes.ContainsKey(mathOperator.right))
                    {
                        var outputPort = (nodes[mathOperator.right] as Node).outputContainer[0] as Port;
                        var inputPort = graphNode.inputContainer[1] as Port;

                        Edge edge = inputPort.ConnectTo(outputPort);
                        edge.viewDataKey = mathOperator.right.nodeID + "_edge";
                        AddElement(edge);
                    }
                    else if (mathOperator.right != null)
                    {
                        Debug.LogWarning("Invalid right operand for operator " + mathOperator + " , " + mathOperator.right);
                    }
                }
                else if (mathNode is MathFunction)
                {
                    MathFunction mathFunction = mathNode as MathFunction;

                    if (!nodes.ContainsKey(mathNode))
                    {
                        Debug.LogError("No element found for " + mathNode);
                        continue;
                    }

                    var graphNode = nodes[mathNode] as Node;

                    for (int i = 0; i < mathFunction.parameterCount; ++i)
                    {
                        MathNode param = mathFunction.GetParameter(i);

                        if (param != null && nodes.ContainsKey(param))
                        {
                            var outputPort = (nodes[param] as Node).outputContainer[0] as Port;
                            var inputPort = graphNode.inputContainer[i] as Port;

                            Edge edge = inputPort.ConnectTo(outputPort);
                            edge.viewDataKey = param.nodeID + "_edge";
                            AddElement(edge);
                        }
                        else if (param != null)
                        {
                            Debug.LogWarning("Invalid parameter for function" + mathFunction + " , " +
                                param);
                        }
                    }
                }
                else if (mathNode is MathResult)
                {
                    MathResult mathResult = mathNode as MathResult;
                    var graphNode = nodes[mathNode] as Node;

                    if (mathResult.root != null)
                    {
                        var outputPort = (nodes[mathResult.root] as Node).outputContainer[0] as Port;
                        var inputPort = graphNode.inputContainer[0] as Port;

                        Edge edge = inputPort.ConnectTo(outputPort);
                        edge.viewDataKey = mathResult.root.nodeID + "_edge";
                        AddElement(edge);
                    }
                }
            }

            foreach (var matModel in placemats.OrderBy(p => p.zOrder))
            {
                var newPlacemat = placematContainer.CreatePlacemat<SimplePlacemat>(matModel.position, matModel.zOrder, matModel.title);
                newPlacemat.userData = matModel;
                newPlacemat.viewDataKey = matModel.identification;
                newPlacemat.Model = matModel;

                if (newToOldIdMapping.TryGetValue(matModel.identification, out oldId))
                {
                    oldIdToNewNode.Add(oldId, newPlacemat);
                }
            }

            if (stickies != null)
            {
                var existingStickies = this.Query<SimpleStickyNote>().ToList();

                foreach (var sticky in existingStickies.Where(t => !stickies.Contains(t.model)))
                    RemoveElement(sticky);

                foreach (var stickyModel in stickies.Except(existingStickies.Select(t => t.model)))
                {
                    var newSticky = new SimpleStickyNote();
                    newSticky.model = stickyModel;
                    newSticky.userData = stickyModel;
                    AddElement(newSticky);

                    if (newToOldIdMapping.TryGetValue(stickyModel.id, out oldId))
                    {
                        oldIdToNewNode.Add(oldId, newSticky);
                    }
                }
            }

            // Now that all graph elements have been created, init the collapsed elements of each placemat.
            foreach (var p in this.Query<SimplePlacemat>().ToList())
            {
                p.InitCollapsedElementsFromModel();
            }

            // Make sure collapsed edges are hidden.
            placematContainer.HideCollapsedEdges();

            UpdateSelection(oldIdToNewNode);

            RebuildBlackboard();
        }

        public override Blackboard GetBlackboard()
        {
            m_Blackboard.Rebuild();
            return m_Blackboard;
        }

        public void RebuildBlackboard()
        {
            m_Blackboard.Rebuild();
        }

        public void UpdateSelection(Dictionary<string, ISelectable> oldIdToNewNode)
        {
            if (oldIdToNewNode == null || oldIdToNewNode.Count == 0)
                return;

            if (selection.Count == 0)
            {
                // Select new elements
                foreach (var selectable in oldIdToNewNode)
                {
                    AddToSelection(selectable.Value);
                }

                return;
            }

            // Select the new elements for which the original node is currently selected.
            List<ISelectable> newSelection = new List<ISelectable>();
            foreach (var graphElement in selection.OfType<GraphElement>())
            {
                string oldId = null;

                // Find id of selected element, if possible.
                if (graphElement.userData != null)
                {
                    if (graphElement.userData is MathNode mathNode)
                    {
                        oldId = mathNode.nodeID.ToString();
                    }
                    else if (graphElement.userData is MathPlacemat placemat)
                    {
                        oldId = placemat.identification;
                    }
                    else if (graphElement.userData is MathStickyNote stickyNote)
                    {
                        oldId = stickyNote.id;
                    }
                }

                if (oldId != null)
                {
                    ISelectable element;
                    if (oldIdToNewNode.TryGetValue(oldId, out element))
                    {
                        newSelection.Add(element);
                    }
                }
            }

            if (newSelection.Count > 0)
            {
                ClearSelection();
                foreach (var selectable in newSelection)
                {
                    AddToSelection(selectable);
                }
            }
        }
    }
}
#endif
