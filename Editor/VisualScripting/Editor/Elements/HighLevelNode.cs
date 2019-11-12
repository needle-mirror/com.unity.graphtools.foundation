using System;
#if PROPERTIES
using Unity.Properties;
#endif
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    public class HighLevelNode : Node
    {
#if PROPERTIES
        static readonly HighLevelNodeImguiVisitor m_Visitor = new HighLevelNodeImguiVisitor();
#endif
        static readonly CustomStyleProperty<float> k_LabelWidth = new CustomStyleProperty<float>("--unity-hl-node-label-width");
        static readonly CustomStyleProperty<float> k_FieldWidth = new CustomStyleProperty<float>("--unity-hl-node-field-width");

        const float k_DefaultLabelWidth = 150;
        const float k_DefaultFieldWidth = 120;

        public HighLevelNode(INodeModel model, Store store, GraphView graphView)
            : base(model, store, graphView) {}

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

            var imguiContainer = CreateControls();

            imguiContainer.AddToClassList("node-controls");
            mainContainer.MandatoryQ("top").Insert(1, imguiContainer);
        }

        protected virtual VisualElement CreateControls()
        {
            return new IMGUIContainer(() =>
            {
                EditorGUIUtility.labelWidth = customStyle.TryGetValue(k_LabelWidth, out var labelWidth) ? labelWidth : k_DefaultLabelWidth;
                EditorGUIUtility.fieldWidth = customStyle.TryGetValue(k_FieldWidth, out var fieldWidth) ? fieldWidth : k_DefaultFieldWidth;
#if !PROPERTIES
                EditorGUILayout.LabelField("com.unity.properties is not installed in the project");
#else
                ChangeTracker changeTracker = new ChangeTracker();
                var modelContainer = model;
                m_Visitor.model = model;
                PropertyContainer.Visit(ref modelContainer, m_Visitor, ref changeTracker);
                if (changeTracker.IsChanged())
                    RedefineNode();
#endif
            });
        }
    }
}
