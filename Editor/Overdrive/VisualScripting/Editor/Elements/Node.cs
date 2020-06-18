using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.UIElements;
using IDroppable = UnityEditor.GraphToolsFoundation.Overdrive.GraphElements.IDroppable;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [PublicAPI]
    public class Node : CollapsibleInOutNode, IDroppable, IHighlightable,
        IBadgeContainer, ICustomSearcherHandler, INodeState
    {
        int m_SelectedIndex;
        public int selectedIndex => m_SelectedIndex;

        public bool InstantAdd { get; set; }

        bool HasInstancePort => m_InstancePort != null;

        public IconBadge ErrorBadge { get; set; }
        public ValueBadge ValueBadge { get; set; }
        public NodeUIState UIState { get; set; }

        protected Port m_InstancePort;

        readonly VisualElement m_InsertLoopPortContainer;

        public static readonly string k_TitleIconContainerPartName = "title-icon-container";

        const float k_ByteToPercentFactor = 100 / 255.0f;
        public byte Progress
        {
            set
            {
                var titleComponent = PartList.GetPart(k_TitleIconContainerPartName) as IconTitleProgressPart;
                if (titleComponent?.CoroutineProgressBar != null)
                {
                    titleComponent.CoroutineProgressBar.value = value * k_ByteToPercentFactor;
                }
            }
        }

        public new Store Store => base.Store as Store;
        public new NodeModel NodeModel => base.NodeModel as NodeModel;
        VisualElement m_ContentContainer;
        public override VisualElement contentContainer => m_ContentContainer ?? this;

        protected override void BuildPartList()
        {
            base.BuildPartList();

            PartList.ReplacePart(k_TitleContainerPartName, IconTitleProgressPart.Create(k_TitleIconContainerPartName, Model, this, k_UssClassName));
        }

        protected override void BuildElementUI()
        {
            m_ContentContainer = this.AddBorder(k_UssClassName);
            base.BuildElementUI();
            this.AddOverlay();
        }

        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "Node.uss"));
        }

        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            viewDataKey = NodeModel.GetId();

            EnableInClassList("has-instance-port", HasInstancePort);

            UIState = NodeModel.State == ModelState.Disabled ? NodeUIState.Disabled : NodeUIState.Enabled;
            this.ApplyNodeState();

            tooltip = NodeModel.ToolTip;
        }

        public override void UpdatePinning()
        {
            m_SelectedIndex = -1;
        }

        public override bool IsDroppable()
        {
            var nodeParent = parent as GraphElement;
            var nodeParentSelected = nodeParent?.IsSelected(GraphView) ?? false;
            return base.IsDroppable() && !nodeParentSelected;
        }

        public override bool IsSelected(VisualElement selectionContainer)
        {
            return GraphView.selection.Contains(this);
        }

        public IGraphElementModel GraphElementModel => NodeModel;

        public bool Highlighted
        {
            get => ClassListContains("highlighted");
            set => EnableInClassList("highlighted", value);
        }

        public bool ShouldHighlightItemUsage(IGraphElementModel graphElementModel)
        {
            return false;
        }

        public Func<Node, Store, Vector2, SearcherFilter, bool> CustomSearcherHandler { get; set; }

        public bool HandleCustomSearcher(Vector2 mousePosition, SearcherFilter filter = null)
        {
            if (CustomSearcherHandler != null)
                return CustomSearcherHandler(this, Store, mousePosition, filter);

            return true;
        }
    }
}
