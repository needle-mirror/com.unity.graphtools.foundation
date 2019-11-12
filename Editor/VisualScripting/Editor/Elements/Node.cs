using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Searcher;
using UnityEditor.UIElements;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    [PublicAPI]
    public class Node : Experimental.GraphView.Node, IDroppable, IHasGraphElementModel, IHighlightable,
        IBadgeContainer, ICustomColor, IMovable, ICustomSearcherHandler, INodeState
    {
        protected readonly Store m_Store;
        public readonly INodeModel model;

        int m_SelectedIndex;
        public int selectedIndex => m_SelectedIndex;

        public bool InstantAdd { get; set; }

        public StackNode Stack => GetFirstAncestorOfType<StackNode>();

        public bool HasInstancePort => m_InstancePort != null;

        public override string title
        {
            get => m_TitleLabel != null ? m_TitleLabel.text.Nicify() : base.title;
            set
            {
                if (m_TitleLabel != null)
                    m_TitleLabel.text = value;
                else
                    base.title = value;
            }
        }

        public IconBadge ErrorBadge { get; set; }
        public ValueBadge ValueBadge { get; set; }
        public NodeUIState UIState { get; set; }

        protected Port m_InstancePort;

        readonly VisualElement m_InsertLoopPortContainer;

        ProgressBar m_CoroutineProgressBar;
        const float k_ByteToPercentFactor = 100 / 255.0f;
        public byte Progress
        {
            set
            {
                if (m_CoroutineProgressBar != null)
                {
                    m_CoroutineProgressBar.value = value * k_ByteToPercentFactor;
                }
            }
        }

#if UNITY_2019_3_OR_NEWER
        Label m_TitleLabel;
#else
        BoundLabel m_TitleLabel;
#endif
        protected VisualElement TitleContainer { get; private set; }

        protected GraphView m_GraphView;

        public Node(INodeModel model, Store store, GraphView graphView, string file = "UXML/GraphView/Node.uxml") : base(file)
        {
            UseDefaultStyling();
            Assert.IsNotNull(model);
            Assert.IsNotNull(store);

            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "Node.uss"));
            // @TODO: This might need to be reviewed in favor of a better / more scalable approach (non preprocessor based)
            // that would ideally bring the same level of backward/forward compatibility and/or removed when a 2013 beta version lands.
#if UNITY_2019_3_OR_NEWER
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "Node.2019.3.uss"));
#endif
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "PropertyField.uss"));

            this.model = model;
            m_Store = store;
            m_GraphView = graphView;

            if (model.IsInsertLoop)
            {
                style.overflow = Overflow.Visible;
                AddToClassList("insertLoopNode");

                m_InsertLoopPortContainer = new VisualElement {name = "insertLoopPortContainer"};
                m_InsertLoopPortContainer.style.overflow = Overflow.Visible;
                m_InsertLoopPortContainer.pickingMode = PickingMode.Ignore;

                VisualElement nodeBorder = this.MandatoryQ("node-border");
                nodeBorder.style.overflow = Overflow.Visible;

                VisualElement titleElement = this.MandatoryQ("title");
                titleElement.style.overflow = Overflow.Visible;
                titleElement.EnableInClassList("hasDataInputPorts",
                    model.InputsByDisplayOrder.Any(x => x.PortType == PortType.Data));

                titleElement.Add(m_InsertLoopPortContainer);

                var loopIconContainer = new VisualElement {name = "loopIconContainer"};
                loopIconContainer.Add(new VisualElement {name = "loopIcon"});
                titleContainer.Add(loopIconContainer);
            }

            UpdateFromModel();

            var titleLabel = HasInstancePort ? m_InstancePort.Q<Label>(className: "connectorText") : this.Q<Label>("title-label");
            if (model is INodeModelProgress)
                m_CoroutineProgressBar = new ProgressBar();
            if (HasInstancePort)
            {
                titleLabel.text = model.Title;
                this.Q<Label>("title-label").RemoveFromHierarchy();
                if (m_CoroutineProgressBar != null)
                    titleLabel.parent.Add(m_CoroutineProgressBar);
            }
            else
            {
                var titleParent = titleLabel.parent;
                TitleContainer = new VisualElement {name = "titleContainer"};

                var nodeIcon = new Image { name = "nodeIcon" };
                nodeIcon.AddToClassList(((NodeModel)model).IconTypeString);
                TitleContainer.Add(nodeIcon);
                TitleContainer.Add(titleLabel);
                if (m_CoroutineProgressBar != null)
                    TitleContainer.Add(m_CoroutineProgressBar);

                titleParent.Insert(0, TitleContainer);
            }

            if (model is IObjectReference modelReference)
            {
                EnableInClassList("invalid", modelReference.ReferencedObject == null);

                titleLabel.text = model.Title;

                if (modelReference is IExposeTitleProperty titleProperty)
                {
                    m_TitleLabel = titleLabel;
                }
            }

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            RefreshExpandedState();
        }

        protected virtual void UpdateFromModel()
        {
            capabilities = VseUtility.ConvertCapabilities(model);

            string nodeTitle = model.Title ?? "";

            if (model.ParentStackModel == null)
            {
                AddToClassList("standalone");
                SetPosition(new Rect(model.Position, Vector2.zero));
            }
            else
            {
                AddToClassList("stackable-node");
            }

            UpdateInputPortModels();
            UpdateOutputPortModels();

            // Needs to come after ports are set.
            title = nodeTitle;

            viewDataKey = model.GetId();
        }

        protected virtual void UpdateInputPortModels()
        {
            foreach (IPortModel inputPortModel in model.InputsByDisplayOrder)
            {
                Port port = Port.CreateInputPort(m_Store, inputPortModel, titleContainer, inputContainer);

                if (inputPortModel.PortType == PortType.Instance)
                {
                    AddToClassList("instance");
                    m_InstancePort = port;
                }
            }
        }

        protected virtual void UpdateOutputPortModels()
        {
            if (model.IsBranchType)
                return;

            foreach (var outputPortModel in model.OutputsByDisplayOrder)
            {
                var port = Port.Create(outputPortModel, m_Store, Orientation.Horizontal);
                if (outputPortModel.PortType == PortType.Loop && model.IsInsertLoop && m_InsertLoopPortContainer != null)
                {
                    // Convert port to InsertLoop type port
                    port.portName = "";
                    port.AddToClassList("loop");
                    m_InsertLoopPortContainer.Add(port);
                }
                else
                {
                    outputContainer.Add(port);
                }
            }
        }

        public void UpdatePinning()
        {
            m_SelectedIndex = -1;
        }

        public bool NeedStoreDispatch => ClassListContains("standalone");

        public bool IsInStack => !(ClassListContains("standalone"));

        public int FindIndexInStack()
        {
            // Find index of child so we can provide with the correct information
            var nodes = Stack?.Query<Node>().ToList();
            // uQuery can return null lists...
            if (nodes != null)
            {
                for (var i = 0; i < nodes.Count; i++)
                {
                    if (this == nodes[i])
                        return i;
                }
            }

            return -1;
        }

        void OnAttachToPanel(AttachToPanelEvent e)
        {
            if (model is IObjectReference modelReference && modelReference.ReferencedObject != null)
            {
                m_TitleLabel?.Bind(new SerializedObject(modelReference.ReferencedObject));
            }
        }

        void OnDetachFromPanel(DetachFromPanelEvent e)
        {
            m_TitleLabel?.Unbind();
        }

        public override void SetPosition(Rect newPos)
        {
            SetPositionPrivateImpl(newPos);
        }

        void SetPositionPrivateImpl(Rect newPos)
        {
            if (IsDroppable())
                base.SetPosition(new Rect(newPos.position, layout.size));
        }

        public override bool IsDroppable()
        {
            var nodeParent = parent as GraphElement;
            var nodeParentSelected = nodeParent?.IsSelected(m_GraphView) ?? false;
            return base.IsDroppable() && !nodeParentSelected;
        }

        public override void OnSelected()
        {
            base.OnSelected();
            if (IsInStack && m_SelectedIndex == -1)
                m_SelectedIndex = FindIndexInStack();
        }

        public override bool IsSelected(VisualElement selectionContainer)
        {
            return m_GraphView.selection.Contains(this);
        }

        public IGraphElementModel GraphElementModel => model;

        public bool Highlighted
        {
            get => ClassListContains("highlighted");
            set => EnableInClassList("highlighted", value);
        }

        public bool ShouldHighlightItemUsage(IGraphElementModel graphElementModel)
        {
            return false;
        }

        public virtual void SetColor(Color c)
        {
            var titleElement = this.MandatoryQ("title");
            titleElement.style.backgroundColor = c;
        }

        public Func<Node, Store, Vector2, SearcherFilter, bool> CustomSearcherHandler { get; set; }

        public bool HandleCustomSearcher(Vector2 mousePosition, SearcherFilter filter = null)
        {
            if (CustomSearcherHandler != null)
                return CustomSearcherHandler(this, m_Store, mousePosition, filter);

            // TODO: Refactor this and use interface to manage nodeModel->searcher mapping
            if (!model.IsStacked || GraphElementModel is PropertyGroupBaseNodeModel)
            {
                return false;
            }

            SearcherService.ShowStackNodes(m_Store.GetState(), model.ParentStackModel, mousePosition, item =>
            {
                m_Store.Dispatch(new ChangeStackedNodeAction(model, model.ParentStackModel, item));
            }, new SearcherAdapter($"Change this {model.Title}"));

            return true;
        }

        protected void RedefineNode()
        {
            ((NodeModel)model).DefineNode();
            m_Store.Dispatch(new RefreshUIAction(UpdateFlags.RequestCompilation | UpdateFlags.GraphTopology, new List<IGraphElementModel> { model }));
        }
    }
}
