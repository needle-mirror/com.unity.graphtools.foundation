using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    internal struct InsertInfo
    {
        public static readonly InsertInfo nil = new InsertInfo { target = null, index = -1, localPosition = Vector2.zero };
        public VisualElement target;
        public int index;
        public Vector2 localPosition;
    }

    internal interface IInsertLocation
    {
        void GetInsertInfo(Vector2 worldPosition, out InsertInfo insertInfo);
    }
}
