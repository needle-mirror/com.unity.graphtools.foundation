using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class ErrorToolbar : Toolbar
    {
        VisualElement m_ErrorIconLabel;
        ToolbarButton m_PreviousErrorButton;
        ToolbarButton m_NextErrorButton;
        Label m_ErrorCounterLabel;

        public ErrorToolbar(Store store, GraphView graphView) : base(store, graphView)
        {
            name = "errorToolbar";
            this.AddStylesheet("ErrorToolbar.uss");

            var tpl = GraphElementHelper.LoadUXML("ErrorToolbar.uxml");
            tpl.CloneTree(this);

            m_ErrorIconLabel = this.MandatoryQ("errorIconLabel");

            m_PreviousErrorButton = this.MandatoryQ<ToolbarButton>("previousErrorButton");
            m_PreviousErrorButton.tooltip = "Go To Previous Error";
            m_PreviousErrorButton.ChangeClickEvent(OnPreviousErrorButton);

            m_NextErrorButton = this.MandatoryQ<ToolbarButton>("nextErrorButton");
            m_NextErrorButton.tooltip = "Go To Next Error";
            m_NextErrorButton.ChangeClickEvent(OnNextErrorButton);

            m_ErrorCounterLabel = this.MandatoryQ<Label>("errorCounterLabel");
        }

        void OnPreviousErrorButton()
        {
            m_GraphView.FramePrev(HasErrorBadge);
        }

        void OnNextErrorButton()
        {
            m_GraphView.FrameNext(HasErrorBadge);
        }

        static bool HasErrorBadge(GraphElement element)
        {
            return element.ClassListContains(ErrorBadge.hasErrorUssClassName);
        }

        public void UpdateUI()
        {
            bool enabled = m_Store.State.GraphModel != null;

            int errorCount = 0;

            IGraphModel graphModel = m_Store.State.GraphModel;
            if (graphModel != null)
            {
                errorCount = (m_Store.State.CompilationStateComponent.GetLastResult()?.errors?.Count).GetValueOrDefault(0);
            }

            enabled &= errorCount > 0;

            m_ErrorIconLabel.SetEnabled(enabled);
            m_PreviousErrorButton.SetEnabled(enabled);
            m_NextErrorButton.SetEnabled(enabled);

            m_ErrorCounterLabel.SetEnabled(enabled);
            m_ErrorCounterLabel.text = errorCount + (errorCount == 1 ? " error" : " errors");
        }
    }
}
