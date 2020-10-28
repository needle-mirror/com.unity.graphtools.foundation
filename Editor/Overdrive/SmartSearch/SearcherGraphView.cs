using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class SearcherGraphView : GraphView
    {
        public SearcherGraphView(GraphViewEditorWindow window, Store store) : base(window, store)
        {
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(PackageTransitionHelper.AssetPath +
                "SmartSearch/Stylesheets/SearcherGraphView.uss"));

            AddToClassList("searcher-graph-view");

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
