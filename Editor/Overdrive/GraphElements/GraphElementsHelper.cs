using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public static class GraphElementsHelper
    {
        public static string WithUssElement(this string blockName, string elementName) => blockName + "__" + elementName;
        public static string WithUssModifier(this string blockName, string modifier) => blockName + "--" + modifier;

        public static void LoadTemplateAndStylesheet(VisualElement container, string name, string rootClassName, IEnumerable<string> additionalStylesheets = null)
        {
            if (name != null && container != null)
            {
                var tpl = LoadUXML(name + ".uxml");
                tpl.CloneTree(container);

                if (additionalStylesheets != null)
                {
                    foreach (var additionalStylesheet in additionalStylesheets)
                    {
                        container.AddStylesheet(additionalStylesheet + ".uss");
                    }
                }

                container.AddStylesheet(name + ".uss");

#if !UNITY_2020_1_OR_NEWER
                container.AddToClassList(rootClassName);
#endif
            }
        }

        static string StylesheetPath = PackageTransitionHelper.AssetPath + "GraphElements/Stylesheets/";
        static string NewLookStylesheetPath = PackageTransitionHelper.AssetPath + "GraphElements/Stylesheets/NewLook/";
        static string TemplatePath = PackageTransitionHelper.AssetPath + "GraphElements/Templates/";
        static internal bool UseNewStylesheets { get; set; } = false;

        public static void AddStylesheet(this VisualElement ve, string stylesheetName)
        {
            StyleSheet stylesheet = null;

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (UseNewStylesheets)
                stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(NewLookStylesheetPath + stylesheetName);

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (stylesheet == null)
            {
                stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylesheetPath + stylesheetName);
            }

            if (stylesheet != null)
            {
                ve.styleSheets.Add(stylesheet);
            }
            else
            {
                Debug.Log("Failed to load stylesheet " + StylesheetPath + stylesheetName);
            }
        }

        public static VisualTreeAsset LoadUXML(string uxmlName)
        {
            var tpl = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TemplatePath + uxmlName);
            if (tpl == null)
            {
                Debug.Log("Failed to load template " + TemplatePath + uxmlName);
            }

            return tpl;
        }
    }

    static class StringUtilsExtensions
    {
        static readonly char NoDelimiter = '\0'; //invalid character

        public static string ToPascalCase(this string text)
        {
            return ConvertCase(text, NoDelimiter, char.ToUpperInvariant, char.ToUpperInvariant);
        }

        public static string ToCamelCase(this string text)
        {
            return ConvertCase(text, NoDelimiter, char.ToLowerInvariant, char.ToUpperInvariant);
        }

        public static string ToKebabCase(this string text)
        {
            return ConvertCase(text, '-', char.ToLowerInvariant, char.ToLowerInvariant);
        }

        public static string ToTrainCase(this string text)
        {
            return ConvertCase(text, '-', char.ToUpperInvariant, char.ToUpperInvariant);
        }

        public static string ToSnakeCase(this string text)
        {
            return ConvertCase(text, '_', char.ToLowerInvariant, char.ToLowerInvariant);
        }

        static readonly char[] k_WordDelimiters = { ' ', '-', '_' };

        static string ConvertCase(string text,
            char outputWordDelimiter,
            Func<char, char> startOfStringCaseHandler,
            Func<char, char> middleStringCaseHandler)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var builder = new StringBuilder();

            bool startOfString = true;
            bool startOfWord = true;
            bool outputDelimiter = true;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (((IList)k_WordDelimiters).Contains(c))
                {
                    if (c == outputWordDelimiter)
                    {
                        builder.Append(outputWordDelimiter);
                        //we disable the delimiter insertion
                        outputDelimiter = false;
                    }
                    startOfWord = true;
                }
                else if (!char.IsLetterOrDigit(c))
                {
                    startOfString = true;
                    startOfWord = true;
                }
                else
                {
                    if (startOfWord || char.IsUpper(c))
                    {
                        if (startOfString)
                        {
                            builder.Append(startOfStringCaseHandler(c));
                        }
                        else
                        {
                            if (outputDelimiter && outputWordDelimiter != NoDelimiter)
                            {
                                builder.Append(outputWordDelimiter);
                            }
                            builder.Append(middleStringCaseHandler(c));
                            outputDelimiter = true;
                        }
                        startOfString = false;
                        startOfWord = false;
                    }
                    else
                    {
                        builder.Append(c);
                    }
                }
            }

            return builder.ToString();
        }
    }
}
