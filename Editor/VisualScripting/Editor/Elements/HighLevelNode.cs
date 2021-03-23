using Unity.Properties.UI;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    public class HighLevelNode : Node
    {
        public HighLevelNode(INodeModel model, Store store, GraphView graphView)
            : base(model, store, graphView) { }

        protected override void UpdateFromModel()
        {
            base.UpdateFromModel();

            AddToClassList("highLevelNode");

            VisualElement topHorizontalDivider = this.MandatoryQ("divider", "horizontal");
            VisualElement topVerticalDivider = this.MandatoryQ("divider", "vertical");

            // GraphView automatically hides divider since there are no input ports
            topHorizontalDivider.RemoveFromClassList("hidden");
            topVerticalDivider.RemoveFromClassList("hidden");

            VisualElement output = this.MandatoryQ("output");
            output.AddToClassList("node-controls");

            var controlsElement = CreateControls();

            controlsElement.AddToClassList("node-controls");
            mainContainer.MandatoryQ("top").Insert(1, controlsElement);
        }

        protected virtual VisualElement CreateControls()
        {
            var element = new PropertyElement();
            element.SetTarget(model);
            element.OnChanged += (e, p) =>
            {
                RedefineNode();
            };
            return element;
        }
    }
}
