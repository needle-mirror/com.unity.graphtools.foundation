using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class SearcherGraphView : GraphView
    {
        public new static readonly string ussClassName = "ge-searcher-graph-view";

        public SearcherGraphView(GraphViewEditorWindow window, Store store) : base(window, store)
        {
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetHelper.AssetPath +
                "SmartSearch/Stylesheets/SearcherGraphView.uss"));

            AddToClassList(ussClassName);

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
