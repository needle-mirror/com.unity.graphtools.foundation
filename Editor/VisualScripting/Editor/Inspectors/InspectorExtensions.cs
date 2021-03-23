using System;
using JetBrains.Annotations;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    [PublicAPI]
    static class InspectorExtensions
    {
        [Flags]
        public enum TypeOptions
        {
            None = 0,
            AllowArray = 1,
        }

        static Rect s_ButtonRect;

        public static void TypeEditor(this Stencil stencil, TypeHandle typeHandle, Action<TypeHandle, int> onSelection,
            SearcherFilter filter = null, TypeOptions options = TypeOptions.None)
        {
            var missingTypeReference = TypeHandle.MissingType;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Type");

            var selected = EditorGUILayout.DropdownButton(new GUIContent(typeHandle != missingTypeReference ? typeHandle.GetMetadata(stencil).FriendlyName : "<unknown type>"), FocusType.Passive, GUI.skin.button);
            if (Event.current.type == EventType.Repaint)
            {
                s_ButtonRect = GUILayoutUtility.GetLastRect();
            }

            if (selected)
            {
                SearcherService.ShowTypes(
                    stencil,
                    EditorWindow.focusedWindow.rootVisualElement.LocalToWorld(s_ButtonRect.center),
                    onSelection,
                    filter
                );
            }
            EditorGUILayout.EndHorizontal();
        }

        public static void NameEditor(this UnityEditor.Editor editor, ScriptableObject obj)
        {
            obj.name = EditorGUILayout.DelayedTextField("Name", obj.name);
        }

        public static void VariableNameEditor(this UnityEditor.Editor editor, VariableDeclarationModel variableDeclaration)
        {
            var newName = EditorGUILayout.DelayedTextField("Name", variableDeclaration.Title);
            if (newName == variableDeclaration.Title)
                return;

            variableDeclaration.SetNameFromUserName(newName);
        }
    }
}
