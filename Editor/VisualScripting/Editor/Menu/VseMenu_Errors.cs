using System;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    partial class VseMenu
    {
        VisualElement m_ErrorIconLabel;
        ToolbarButton m_PreviousErrorButton;
        VisualElement m_NextErrorButton;
        Label m_ErrorCounterLabel;

        void CreateErrorMenu()
        {
            m_ErrorIconLabel = this.MandatoryQ("errorIconLabel");

            m_PreviousErrorButton = this.MandatoryQ<ToolbarButton>("previousErrorButton");
            m_PreviousErrorButton.tooltip = "Go To Previous Error";
            m_PreviousErrorButton.RemoveManipulator(m_PreviousErrorButton.clickable);
            m_PreviousErrorButton.AddManipulator(new Clickable(OnPreviousErrorButton));

            m_NextErrorButton = this.MandatoryQ("nextErrorButton");
            m_NextErrorButton.tooltip = "Go To Next Error";
            m_NextErrorButton.AddManipulator(new Clickable(OnNextErrorButton));

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
            return element.ClassListContains("hasErrorIconBadge");
        }

        void UpdateErrorMenu(bool enabled)
        {
            int errorCount = 0;

            IGraphModel graphModel = m_Store.GetState().CurrentGraphModel;
            if (graphModel != null)
            {
                if (m_Store.GetState().CompilationResultModel != null)
                {
                    errorCount = (m_Store.GetState().CompilationResultModel?.GetLastResult()?.errors?.Count).GetValueOrDefault(0);
                }
            }

            enabled &= errorCount > 0;

            m_ErrorIconLabel.SetEnabled(enabled);
            m_PreviousErrorButton.SetEnabled(enabled);
            m_NextErrorButton.SetEnabled(enabled);

            m_ErrorCounterLabel.SetEnabled(enabled);
            m_ErrorCounterLabel.text = errorCount.ToString();
        }
    }
}
