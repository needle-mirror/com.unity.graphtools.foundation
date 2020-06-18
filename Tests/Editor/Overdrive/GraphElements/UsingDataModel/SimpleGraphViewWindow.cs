#if DISABLE_SIMPLE_MATH_TESTS
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using Unity.GraphElements;
using Unity.GraphToolsFoundation.Model;
using Unity.GraphToolsFoundations.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.UsingDataModel.NoPresenters
{
    internal partial class SimpleGraphViewWindowTools : SimpleGraphViewWindow
    {
        [MenuItem("GraphView/SimpleGraph")]
        public static void ShowWindow()
        {
            GetWindow<SimpleGraphViewWindowTools>();
        }
    }

    internal partial class SimpleGraphViewWindow : GraphViewEditorWindow, ISearchWindowProvider
    {
        public GraphView graphView { get; private set; }
        private MathBook m_MathBook;
        private SimpleGraphViewCallbacks m_SimpleGraphViewCallbacks = new SimpleGraphViewCallbacks();
        private StackNode m_InsertStack;
        private int m_InsertIndex;

        public MathBook mathBook => m_MathBook;

        protected virtual bool withWindowedTools => false;

        public override IEnumerable<GraphView> graphViews
        {
            get { yield return graphView; }
        }

        public virtual void OnEnable()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(MathBookHelper.GetAssetPath());
            if (assets == null || assets.Length == 0)
            {
                MathBookHelper.ResetOrCreateAsset();
                assets = AssetDatabase.LoadAllAssetsAtPath(MathBookHelper.GetAssetPath());

                if (assets == null || assets.Length == 0)
                {
                    Debug.LogError("Could not load Math asset.");
                    return;
                }
            }

            // Find the MathBook.
            foreach (var asset in assets)
            {
                if (asset is MathBook)
                {
                    m_MathBook = asset as MathBook;
                    break;
                }
            }

            var simpleGraphView = new SimpleGraphView(this, withWindowedTools);
            graphView = simpleGraphView;

            graphView.name = "MathBook";
            graphView.viewDataKey = "MathBook";
            graphView.StretchToParentSize();

            rootVisualElement.Add(graphView);
            graphView.AddStylesheet("SimpleGraph");

            m_SimpleGraphViewCallbacks.Init(m_MathBook, simpleGraphView);

            graphView.nodeCreationRequest += OnRequestNodeCreation;

            titleContent.text = "Simple Graph";

            Reload();
        }

        public void OnDisable()
        {
            m_SimpleGraphViewCallbacks.DeInit((SimpleGraphView)graphView);
        }

        protected void OnRequestNodeCreation(NodeCreationContext context)
        {
            m_InsertStack = context.target as StackNode;
            m_InsertIndex = context.index;
            SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), this);
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>();

            Texture2D icon = GraphViewStaticBridge.LoadIconRequired("cs Script Icon");

            tree.Add(new SearchTreeGroupEntry(new GUIContent("Create Node"), 0));

            tree.Add(new SearchTreeGroupEntry(new GUIContent("Operators"), 1));
            tree.Add(new SearchTreeEntry(new GUIContent("Addition", icon)) { level = 2, userData = typeof(MathAdditionOperator) });
            tree.Add(new SearchTreeEntry(new GUIContent("Subtraction", icon)) { level = 2, userData = typeof(MathSubtractionOperator) });
            tree.Add(new SearchTreeEntry(new GUIContent("Multiplication", icon)) { level = 2, userData = typeof(MathMultiplicationOperator) });
            tree.Add(new SearchTreeEntry(new GUIContent("Division", icon)) { level = 2, userData = typeof(MathDivisionOperator) });
            tree.Add(new SearchTreeEntry(new GUIContent("Result", icon)) { level = 2, userData = typeof(MathResult) });

            tree.Add(new SearchTreeGroupEntry(new GUIContent("Functions"), 1));
            tree.Add(new SearchTreeEntry(new GUIContent("Sin", icon)) { level = 2, userData = typeof(SinFunction) });
            tree.Add(new SearchTreeEntry(new GUIContent("Asin", icon)) { level = 2, userData = typeof(AsinFunction) });
            tree.Add(new SearchTreeEntry(new GUIContent("Cos", icon)) { level = 2, userData = typeof(CosFunction) });
            tree.Add(new SearchTreeEntry(new GUIContent("Acos", icon)) { level = 2, userData = typeof(AcosFunction) });
            tree.Add(new SearchTreeEntry(new GUIContent("Tan", icon)) { level = 2, userData = typeof(TanFunction) });
            tree.Add(new SearchTreeEntry(new GUIContent("Atan", icon)) { level = 2, userData = typeof(AtanFunction) });
            tree.Add(new SearchTreeEntry(new GUIContent("Min", icon)) { level = 2, userData = typeof(MinFunction) });
            tree.Add(new SearchTreeEntry(new GUIContent("Max", icon)) { level = 2, userData = typeof(MaxFunction) });
            tree.Add(new SearchTreeEntry(new GUIContent("Clamp", icon)) { level = 2, userData = typeof(ClampFunction) });
            tree.Add(new SearchTreeEntry(new GUIContent("Exp", icon)) { level = 2, userData = typeof(ExpFunction) });
            tree.Add(new SearchTreeEntry(new GUIContent("Log", icon)) { level = 2, userData = typeof(LogFunction) });
            tree.Add(new SearchTreeEntry(new GUIContent("Pow", icon)) { level = 2, userData = typeof(PowFunction) });
            tree.Add(new SearchTreeEntry(new GUIContent("Round", icon)) { level = 2, userData = typeof(RoundFunction) });
            tree.Add(new SearchTreeEntry(new GUIContent("Sqrt", icon)) { level = 2, userData = typeof(SqrtFunction) });

            tree.Add(new SearchTreeGroupEntry(new GUIContent("Values"), 1));
            tree.Add(new SearchTreeEntry(new GUIContent("Constant value", icon)) { level = 2, userData = typeof(MathConstant) });
            tree.Add(new SearchTreeEntry(new GUIContent("PI", icon)) { level = 2, userData = typeof(PIConstant) });

            return tree;
        }

        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            if (!(entry is SearchTreeGroupEntry))
            {
                if (!graphView.HasGUIView())
                    return false;

                MathNode node = ScriptableObject.CreateInstance(entry.userData as Type) as MathNode;

                AddNode(node);
                Node nodeUI = CreateNode(node) as Node;
                if (nodeUI != null)
                {
                    if (m_InsertStack != null)
                    {
                        MathStackNode stackNode = m_InsertStack.userData as MathStackNode;

                        stackNode.InsertNode(m_InsertIndex, node);
                        m_InsertStack.InsertElement(m_InsertIndex, nodeUI);
                    }
                    else
                    {
                        graphView.AddElement(nodeUI);

                        Vector2 pointInWindow = context.screenMousePosition - position.position;
                        Vector2 pointInGraph = nodeUI.parent.WorldToLocal(pointInWindow);

                        nodeUI.SetPosition(new Rect(pointInGraph, Vector2.zero)); // it's ok to pass zero here because width/height is dynamic
                    }
                    nodeUI.Select(graphView, false);
                }
                else
                {
                    Debug.LogError("Failed to create element for " + node);
                    return false;
                }

                return true;
            }
            return false;
        }

        private Node CreateMathNode(MathNode mathNode, string title, Vector2 pos, int inputs, int outputs)
        {
            SimpleNode node = new SimpleNode();
            node.userData = mathNode;
            node.viewDataKey = mathNode.nodeID.ToString();

            for (int i = 0; i < inputs; ++i)
            {
                var inputPort = node.InstantiatePort(Orientation.Horizontal, Direction.Input, PortCapacity.Single, typeof(float));
                inputPort.userData = mathNode;
                node.inputContainer.Add(inputPort);
            }

            for (int i = 0; i < outputs; ++i)
            {
                var outputPort = node.InstantiatePort(Orientation.Horizontal, Direction.Output, PortCapacity.Multi, typeof(float));
                outputPort.userData = mathNode;
                node.outputContainer.Add(outputPort);
            }

            node.SetPosition(new Rect(pos.x, pos.y, 100, 100));
            node.title = title;
            node.RefreshPorts();

            return node;
        }

        private GraphElement CreateStackNode(MathStackNode mathStackNode, Vector2 pos)
        {
            SimpleStackNode graphStackNode = new SimpleStackNode(mathStackNode);

            graphStackNode.SetPosition(new Rect(pos.x, pos.y, 100, 100));

            return graphStackNode;
        }

        public GraphElement CreateNode(MathNode mathNode)
        {
            if (mathNode is MathOperator)
            {
                MathOperator add = mathNode as MathOperator;

                return CreateMathNode(mathNode, mathNode.name, add.m_Position, 2, 1);
            }
            else if (mathNode is MathStackNode)
            {
                MathStackNode mathStackNode = mathNode as MathStackNode;

                return CreateStackNode(mathStackNode, mathStackNode.m_Position);
            }
            else if (mathNode is MathFunction)
            {
                MathFunction fn = mathNode as MathFunction;

                Debug.Assert(fn.parameterCount == fn.parameterNames.Length);

                Node nodeUI = CreateMathNode(mathNode, mathNode.name, mathNode.m_Position, fn.parameterNames.Length, 1);

                for (int i = 0; i < fn.parameterNames.Length; ++i)
                {
                    (nodeUI.inputContainer.ElementAt(i) as Port).portName = fn.parameterNames[i];
                }

                return nodeUI;
            }
            else if (mathNode is IMathBookFieldNode)
            {
                IMathBookFieldNode mathBookFieldNode = mathNode as IMathBookFieldNode;
                SimpleTokenNode tokenNode = new SimpleTokenNode(mathBookFieldNode);

                tokenNode.SetPosition(new Rect(mathNode.m_Position, Vector2.zero));
                tokenNode.RefreshPorts();
                tokenNode.visible = true;

                return tokenNode;
            }
            else if (mathNode is MathConstant)
            {
                MathConstant mathConstant = mathNode as MathConstant;

                Node nodeUI = CreateMathNode(
                    mathNode,
                    mathConstant.name,
                    mathConstant.m_Position, 0, 1);

                var field = new DoubleField() { value = mathConstant.m_Value };
                field.SetEnabled(!(mathConstant is PIConstant));
                field.RegisterValueChangedCallback(evt => mathConstant.m_Value = (float)evt.newValue);
                nodeUI.inputContainer.Add(field);
                nodeUI.RefreshExpandedState();
                return nodeUI;
            }
            else if (mathNode is MathResult)
            {
                MathResult mathResult = mathNode as MathResult;

                Node nodeUI = CreateMathNode(
                    mathNode,
                    "Result",
                    mathResult.m_Position, 1, 0);

                nodeUI.inputContainer.Add(new Button(() => Debug.Log(mathResult.Evaluate())) { text = "Print result" });

                return nodeUI;
            }

            return null;
        }

        public void AddStickyNote(MathStickyNote note)
        {
            if (m_MathBook == null)
                return;

            m_MathBook.Add(note);
            AssetDatabase.AddObjectToAsset(note, m_MathBook);
        }

        public void AddNode(MathNode node)
        {
            if (m_MathBook == null)
                return;

            m_MathBook.Add(node);
            AssetDatabase.AddObjectToAsset(node, m_MathBook);
        }

        public void AddPlacemat(MathPlacemat mat)
        {
            if (m_MathBook == null)
                return;

            m_MathBook.Add(mat);
            AssetDatabase.AddObjectToAsset(mat, m_MathBook);
        }

        public void DestroyNode(MathNode mathNode)
        {
            if (mathNode != null && m_MathBook != null)
            {
                m_MathBook.Remove(mathNode);
                UnityEngine.Object.DestroyImmediate(mathNode, true);
            }
        }

        public void DestroyStickyNote(MathStickyNote note)
        {
            if (note != null)
            {
                m_MathBook.Remove(note);
                UnityEngine.Object.DestroyImmediate(note, true);
            }
        }

        public void DestroyPlacemat(MathPlacemat mat)
        {
            if (mat != null)
            {
                m_MathBook.Remove(mat);
                UnityEngine.Object.DestroyImmediate(mat, true);
            }
        }

        public void Reload()
        {
            if (graphView == null)
                return;

            if (m_MathBook == null)
                return;

            var simpleGraphView = (SimpleGraphView)graphView;
            simpleGraphView.Reload(m_MathBook.nodes, m_MathBook.placemats, m_MathBook.stickyNotes);

            if (!withWindowedTools)
            {
                // Add the minimap.
                var miniMap = new MiniMap();
                miniMap.SetPosition(new Rect(0, 372, 200, 176));
                graphView.Add(miniMap);
            }
        }
    }
}
#endif
