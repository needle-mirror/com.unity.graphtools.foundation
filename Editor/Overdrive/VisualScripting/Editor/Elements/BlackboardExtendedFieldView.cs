using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public abstract class BlackboardExtendedFieldView : VisualElement
    {
        protected readonly Overdrive.Blackboard.RebuildCallback m_RebuildCallback;

        protected void AddRow(string labelText, VisualElement control)
        {
            var row = new VisualElement {name = "blackboardExtendedFieldViewRow"};
            row.AddToClassList("row");

            // TODO: Replace this with a variable pill/token and set isExposed appropriately
            var label = new Label(labelText);
            label.AddToClassList("rowLabel");
            row.Add(label);

            if (control != null)
            {
                control.AddToClassList("rowControl");
                row.Add(control);
            }

            Add(row);
        }

        protected BlackboardExtendedFieldView(IVariableDeclarationModel model, Overdrive.Blackboard.RebuildCallback rebuildCallback)
        {
            userData = model;
            m_RebuildCallback = rebuildCallback;

            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(PackageTransitionHelper.VSTemplatePath + "Blackboard.uss"));

            AddToClassList("blackboardFieldPropertyView");
        }
    }
}
