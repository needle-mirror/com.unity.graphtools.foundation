using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class BlankPage : VisualElement
    {
        public static readonly string ussClassName = "ge-blank-page";

        readonly CommandDispatcher m_CommandDispatcher;

        public IEnumerable<OnboardingProvider> OnboardingProviders { get; protected set; }

        public BlankPage(CommandDispatcher commandDispatcher, IEnumerable<OnboardingProvider> onboardingProviders)
        {
            m_CommandDispatcher = commandDispatcher;
            OnboardingProviders = onboardingProviders;
        }

        public virtual void CreateUI()
        {
            Clear();

            AddToClassList(ussClassName);
            foreach (var provider in OnboardingProviders)
            {
                Add(provider.CreateOnboardingElements(m_CommandDispatcher));
            }
        }

        public virtual void UpdateUI()
        {
        }
    }
}
