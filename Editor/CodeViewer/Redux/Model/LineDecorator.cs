using System;
using UnityEngine;

namespace UnityEditor.CodeViewer
{
    class LineDecorator : ILineDecorator
    {
        public Texture2D Icon { get; }
        public string Tooltip { get; }

        LineDecorator(Texture2D icon, string tooltip)
        {
            Icon = icon;
            Tooltip = tooltip;
        }

        public static LineDecorator CreateError(string tooltip)
        {
            return new LineDecorator(
                EditorGUIUtility.Load("icons/console.erroricon.sml.png") as Texture2D,
                tooltip);
        }

        public static LineDecorator CreateWarning(string tooltip)
        {
            return new LineDecorator(
                EditorGUIUtility.Load("icons/console.warnicon.sml.png") as Texture2D,
                tooltip);
        }

        public static LineDecorator CreateInfo(string tooltip)
        {
            return new LineDecorator(
                EditorGUIUtility.Load("icons/console.infoicon.sml.png") as Texture2D,
                tooltip);
        }
    }
}
