using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    public static class StylesheetsHelper
    {
        static string StylesheetPath = "Packages/com.unity.graphtools.foundation/Tests/Editor/Overdrive/Stylesheets/";

        public static void AddTestStylesheet(VisualElement ve, string stylesheetName)
        {
            StyleSheet stylesheet;
            stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylesheetPath + stylesheetName);
            Assert.IsNotNull(stylesheet);
            ve.styleSheets.Add(stylesheet);
        }
    }
}
