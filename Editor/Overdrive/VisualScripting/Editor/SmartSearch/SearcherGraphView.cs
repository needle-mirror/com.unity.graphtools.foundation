using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch
{
    public class SearcherGraphView : GraphView
    {
        public SearcherGraphView(Store store) : base(store)
        {
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "SearcherGraphView.uss"));

            contentContainer.style.flexBasis = StyleKeyword.Auto;

            AddToClassList("searcherGraphView");

            UnregisterCallback<ValidateCommandEvent>(OnValidateCommand);
            UnregisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
        }

        void OnEnterPanel(AttachToPanelEvent e)
        {
            base.OnEnterPanel();

            panel.visualTree.UnregisterCallback<KeyDownEvent>(OnKeyDownShortcut);
        }
    }
}
