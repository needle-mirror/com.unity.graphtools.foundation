using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    public interface IOnboardingProvider
    {
        string Title { get; }
        VisualElement CreateOnboardingElement(Store store);
    }

    public interface VSOnboardingProvider : IOnboardingProvider {}

    public class VseBlankPage : VisualElement
    {
        static readonly GUIContent k_NoScriptAssetSelectedText = VseUtility.CreatTextContent("Available stencils: ");

        readonly Store m_Store;

        static List<IOnboardingProvider> s_OnboardingProviders;

        protected virtual IEnumerable<IOnboardingProvider> OnboardingProviders
        {
            get
            {
                // new() object of every class inheriting from IOnboardingProvider
                return s_OnboardingProviders ??
                    (s_OnboardingProviders = TypeCache.GetTypesDerivedFrom<VSOnboardingProvider>()
                            .Where(t => t.IsClass && !t.IsAbstract)
                            .Select(templateType => (IOnboardingProvider)Activator.CreateInstance(templateType))
                            .ToList());
            }
        }

        public VseBlankPage(Store store)
        {
            m_Store = store;
        }

        public virtual void UpdateUI()
        {
            Clear();

            Add(new Label { text = k_NoScriptAssetSelectedText.text });

            foreach (var provider in OnboardingProviders)
            {
                Add(new Label { text = "- " + provider.Title });
                Add(provider.CreateOnboardingElement(m_Store));
            }
        }
    }
}
