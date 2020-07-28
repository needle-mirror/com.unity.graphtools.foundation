using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class GraphViewMinimapWindow : GraphViewToolWindow
    {
        static readonly string k_ToolName = "MiniMap";

        MiniMap m_MiniMap;

        Label m_ZoomLabel;

        protected override string ToolName => k_ToolName;

        new void OnEnable()
        {
            base.OnEnable();
            var root = rootVisualElement;
            m_MiniMap = new MiniMap();
            m_MiniMap.Windowed = true;
            m_MiniMap.ZoomFactorTextChanged += ZoomFactorTextChanged;
            root.Add(m_MiniMap);

            OnGraphViewChanged();
            m_ZoomLabel = new Label();
            m_ZoomLabel.style.width = 35;
            m_ZoomLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            m_ToolbarContainer.Add(m_ZoomLabel);
        }

        void OnDestroy()
        {
            if (m_SelectedGraphView != null)
                m_SelectedGraphView.redrawn -= GraphViewRedrawn;
        }

        protected override void OnGraphViewChanging()
        {
            if (m_SelectedGraphView != null)
                m_SelectedGraphView.redrawn -= GraphViewRedrawn;
        }

        protected override void OnGraphViewChanged()
        {
            if (m_SelectedGraphView != null)
                m_SelectedGraphView.redrawn += GraphViewRedrawn;
            else
                ZoomFactorTextChanged("");

            if (m_MiniMap == null) // Probably called from base.OnEnable(). We're not ready just yet.
                return;

            m_MiniMap.GraphView = m_SelectedGraphView;
            m_MiniMap.MarkDirtyRepaint();
        }

        void ZoomFactorTextChanged(string text)
        {
            if (m_ZoomLabel != null)
                m_ZoomLabel.text = text;
        }

        void GraphViewRedrawn()
        {
            m_MiniMap.MarkDirtyRepaint();
        }

        protected override bool IsGraphViewSupported(GraphView gv)
        {
            return true;
        }
    }
}
