using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class BlankPage : VisualElement
    {
        readonly Store m_Store;

        public IEnumerable<IOnboardingProvider> OnboardingProviders { get; protected set; }

        public BlankPage(Store store)
        {
            m_Store = store;
            OnboardingProviders = Enumerable.Empty<IOnboardingProvider>();
        }

        public virtual void CreateUI()
        {
            Clear();

            foreach (var provider in OnboardingProviders)
            {
                Add(provider.CreateOnboardingElement(m_Store));
            }
        }

        public virtual void UpdateUI()
        {
        }
    }
}
