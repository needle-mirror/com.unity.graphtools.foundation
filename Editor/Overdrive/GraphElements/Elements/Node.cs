using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class Node : GraphElement, IHighlightable, IBadgeContainer
    {
        public new static readonly string k_UssClassName = "ge-node";
        public static readonly string k_NotConnectedModifierUssClassName = k_UssClassName.WithUssModifier("not-connected");
        public static readonly string k_EmptyModifierUssClassName = k_UssClassName.WithUssModifier("empty");
        public static readonly string k_DisabledModifierUssClassName = k_UssClassName.WithUssModifier("disabled");
        public static readonly string k_UnusedModifierUssClassName = k_UssClassName.WithUssModifier("unused");
        public static readonly string k_HighlightedModifierUssClassName = k_UssClassName.WithUssModifier("highlighted");
        public static readonly string k_ReadOnlyModifierUssClassName = k_UssClassName.WithUssModifier("read-only");
        public static readonly string k_WriteOnlyModifierUssClassName = k_UssClassName.WithUssModifier("write-only");

        public static readonly string k_SelectionBorderElementName = "selection-border";
        public static readonly string k_DisabledOverlayElementName = "disabled-overlay";
        public static readonly string k_TitleContainerPartName = "title-container";
        public static readonly string k_PortContainerPartName = "port-top-container";

        VisualElement m_ContentContainer;

        public INodeModel NodeModel => Model as INodeModel;

        public override VisualElement contentContainer => m_ContentContainer ?? this;

        public IconBadge ErrorBadge { get; set; }

        public ValueBadge ValueBadge { get; set; }

        public bool Highlighted
        {
            get => ClassListContains(k_HighlightedModifierUssClassName);
            set => EnableInClassList(k_HighlightedModifierUssClassName, value);
        }

        public Node()
        {
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        protected override void BuildPartList()
        {
            PartList.AppendPart(EditableTitlePart.Create(k_TitleContainerPartName, Model, this, k_UssClassName));
            PartList.AppendPart(PortContainerPart.Create(k_PortContainerPartName, Model, this, k_UssClassName));
        }

        protected override void BuildElementUI()
        {
            var selectionBorder = new SelectionBorder { name = k_SelectionBorderElementName };
            selectionBorder.AddToClassList(k_UssClassName.WithUssElement(k_SelectionBorderElementName));
            Add(selectionBorder);
            m_ContentContainer = selectionBorder.ContentContainer;

            base.BuildElementUI();

            var disabledOverlay = new VisualElement { name = k_DisabledOverlayElementName, pickingMode = PickingMode.Ignore };
            hierarchy.Add(disabledOverlay);
        }

        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            usageHints = UsageHints.DynamicTransform;
            AddToClassList(k_UssClassName);
            this.AddStylesheet("Node.uss");
        }

        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            var newPos = NodeModel.Position;
            style.left = newPos.x;
            style.top = newPos.y;

            EnableInClassList(k_EmptyModifierUssClassName, childCount == 0);
            EnableInClassList(k_DisabledModifierUssClassName, NodeModel.State == ModelState.Disabled);

            if (NodeModel is IPortNode portHolder && portHolder.Ports != null)
            {
                bool noPortConnected = portHolder.Ports.All(port => !port.IsConnected());
                EnableInClassList(k_NotConnectedModifierUssClassName, noPortConnected);
            }

            if (Model is IVariableNodeModel variableModel)
            {
                EnableInClassList(k_ReadOnlyModifierUssClassName, variableModel.VariableDeclarationModel?.Modifiers == ModifierFlags.ReadOnly);
                EnableInClassList(k_WriteOnlyModifierUssClassName, variableModel.VariableDeclarationModel?.Modifiers == ModifierFlags.WriteOnly);
            }

            tooltip = NodeModel.Tooltip;

            if (NodeModel.HasUserColor)
            {
                var border = this.MandatoryQ(SelectionBorder.k_ContentContainerElementName);
                border.style.backgroundColor = NodeModel.Color;
                border.style.backgroundImage = null;
            }
            else
            {
                var border = this.MandatoryQ(SelectionBorder.k_ContentContainerElementName);
                border.style.backgroundColor = StyleKeyword.Null;
                border.style.backgroundImage = StyleKeyword.Null;
            }
        }

        internal virtual void UpdateEdges()
        {
            if (NodeModel is IPortNode portContainer && portContainer.Ports != null)
            {
                foreach (var portModel in portContainer.Ports)
                {
                    foreach (var edgeModel in portModel.GetConnectedEdges())
                    {
                        var edge = edgeModel.GetUI<Edge>(GraphView);
                        edge?.UpdateFromModel();
                    }
                }
            }
        }

        void OnGeometryChanged(GeometryChangedEvent e)
        {
            if (e.target == this)
                UpdateEdges();
        }

        public virtual bool ShouldHighlightItemUsage(IGraphElementModel graphElementModel)
        {
            return false;
        }

        // TODO JOCE: This is required until we have a dirtying mechanism (see ShowConnectedExecutionEdgesOrder in NodeModel.cs)
        internal void UpdateOutgoingExecutionEdges()
        {
            foreach (var edge in ((IPortNode)NodeModel).ConnectedPortsWithReorderableEdges().SelectMany(p => p.GetConnectedEdges()))
                edge.GetUI<Edge>(GraphView)?.UpdateFromModel();
        }

        public override void OnSelected()
        {
            base.OnSelected();
            if (!(NodeModel is IPortNode hasPorts))
                return;
            hasPorts.RevealReorderableEdgesOrder(true);
            UpdateOutgoingExecutionEdges();
        }

        public override void OnUnselected()
        {
            base.OnUnselected();

            GraphView.ClearGraphElementsHighlight(ShouldHighlightItemUsage);

            if (!(NodeModel is IPortNode hasPorts))
                return;

            hasPorts.RevealReorderableEdgesOrder(false);
            UpdateOutgoingExecutionEdges();
        }

        public override bool IsSelected(VisualElement selectionContainer)
        {
            return GraphView.Selection.Contains(this);
        }
    }
}
