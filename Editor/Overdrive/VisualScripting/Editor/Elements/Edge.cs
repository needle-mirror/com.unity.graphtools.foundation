using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class Edge : GraphElements.Edge, IHasGraphElementModel
    {
        public EdgeModel VSEdgeModel => EdgeModel as EdgeModel;
        public IGraphElementModel GraphElementModel => VSEdgeModel;

        public static readonly string k_EdgeBubblePartName = "edge-bubble";

        public Edge()
        {
            layer = -1;

            RegisterCallback<AttachToPanelEvent>(OnTargetAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnTargetDetachedFromPanel);
        }

        protected override void BuildPartList()
        {
            base.BuildPartList();
            PartList.AppendPart(EdgeBubblePart.Create(k_EdgeBubblePartName, Model, this, k_UssClassName));
        }

        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            EdgeControl?.RegisterCallback<GeometryChangedEvent>(OnEdgeGeometryChanged);

            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "Edge.uss"));
            viewDataKey = VSEdgeModel?.GetId();
        }

        void OnTargetAttachedToPanel(AttachToPanelEvent evt)
        {
            // PF: Remove this when state has dirty system.
            if (VSEdgeModel?.OutputPortModel != null)
                VSEdgeModel.OutputPortModel.OnValueChanged += OnPortValueChanged;
        }

        void OnTargetDetachedFromPanel(DetachFromPanelEvent evt)
        {
            if (VSEdgeModel?.OutputPortModel != null)
                // ReSharper disable once DelegateSubtraction
                VSEdgeModel.OutputPortModel.OnValueChanged -= OnPortValueChanged;
        }

        void OnPortValueChanged()
        {
            UpdateFromModel();
        }

        void OnEdgeGeometryChanged(GeometryChangedEvent evt)
        {
            var bubblePart = PartList.GetPart(k_EdgeBubblePartName);
            bubblePart.UpdateFromModel();
        }

        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();
            var bubblePart = PartList.GetPart(k_EdgeBubblePartName);
            bubblePart.UpdateFromModel();
        }
    }
}
