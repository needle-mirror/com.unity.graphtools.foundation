using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public interface IOnboardingProvider
    {
        VisualElement CreateOnboardingElement(Store store);
        bool GetGraphAndObjectFromSelection(VseWindow vseWindow, Object selectedObject, out string assetPath,
            out GameObject boundObject);
    }

    public interface VSOnboardingProvider : IOnboardingProvider {}

    public class VseBlankPage : VisualElement
    {
        readonly Store m_Store;

        static List<IOnboardingProvider> s_OnboardingProviders;

        public virtual IEnumerable<IOnboardingProvider> OnboardingProviders
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

        public VseBlankPage()
        {
            AddToClassList("vse-blank-page");
        }

        public VseBlankPage(Store store) : this()
        {
            m_Store = store;
        }

        public virtual void UpdateUI()
        {
            Clear();

            foreach (var provider in OnboardingProviders)
            {
                Add(provider.CreateOnboardingElement(m_Store));
            }
        }
    }
}
