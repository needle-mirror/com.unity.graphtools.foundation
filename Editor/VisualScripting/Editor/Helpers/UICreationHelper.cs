using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.EditorCommon;
using UnityEditor.EditorCommon.Extensions;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    public static class UICreationHelper
    {
        static Dictionary<string, Func<object, Store, VisualElement>> s_Factories;

        static void RegisterFactory(string fullTypeName, Func<object, Store, VisualElement> factory)
        {
            DiscoverFactories();
            s_Factories.Add(fullTypeName, factory);
        }

        static void DiscoverFactories()
        {
            if (s_Factories != null)
                return;

            s_Factories = new Dictionary<string, Func<object, Store, VisualElement>>();

#if UNITY_EDITOR
            AppDomain currentDomain = AppDomain.CurrentDomain;
            foreach (Assembly assembly in currentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypesSafe())
                    {
                        if (type.IsInterface || type.IsAbstract)
                            continue;
                        if (typeof(IUIFactory).IsAssignableFrom(type))
                        {
                            var factory = (IUIFactory)Activator.CreateInstance(type);
                            RegisterFactory(factory.CreatesFromType.FullName, factory.Create);
                        }
                    }
                }
                catch (TypeLoadException e)
                {
                    Debug.LogWarningFormat("Error while loading types from assembly {0}: {1}", assembly.FullName, e);
                }
            }
#endif
        }

        static bool TryGetValue(object model, out Func<object, Store, VisualElement> factory)
        {
            DiscoverFactories();

            Type modelType = model.GetType();

            factory = null;

            do
            {
                s_Factories.TryGetValue(modelType.FullName ?? "", out factory);

                if (factory != null)
                    return true;

                modelType = modelType.BaseType;
            }
            while (modelType != typeof(object) && modelType != null);

            return false;
        }

        internal static VisualElement CreateUIFromModel(object model, Store store)
        {
            if (!TryGetValue(model, out var factory))
            {
                Debug.LogErrorFormat("Model Type '{0}' has no factory method.", model.GetType().FullName);
                return new Label($"Unknown type: '{model.GetType().FullName}'");
            }

            if (factory == null)
            {
                Debug.LogErrorFormat("Visual Element Type '{0}' has a null factory method.", model.GetType().FullName);
                return new Label($"Type with no factory method: '{model.GetType().FullName}'");
            }

            VisualElement res = factory(model, store);
            if (res == null)
            {
                Debug.LogErrorFormat("The factory of Visual Element Type '{0}' has returned a null object", model.GetType().FullName);
                return new Label($"The factory of Visual Element Type '{model.GetType().FullName}' has returned a null object");
            }

            return res;
        }

        public const string templatePath = PackageTransitionHelper.AssetPath + "VisualScripting/Editor/Elements/Templates/";

        public static void CreateFromTemplateAndStyle(VisualElement container, string templateName)
        {
            VisualTreeAsset template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(templatePath + templateName + ".uxml");
            template.CloneTree(container);
            container.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(templatePath + templateName + ".uss"));
            container.ClearClassList();
            container.AddToClassList(templateName);
        }
    }
}
