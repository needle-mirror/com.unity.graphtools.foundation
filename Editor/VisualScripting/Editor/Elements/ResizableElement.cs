using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    [UsedImplicitly]
    public class ResizableElement : VisualElement
    {
        [UsedImplicitly]
        internal new class UxmlFactory : UxmlFactory<ResizableElement> {}

        public ResizableElement()
        {
            VisualTreeAsset template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UICreationHelper.templatePath + "Resizable.uxml");
            template.CloneTree(this);
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "Resizable.uss"));

            foreach (Resizer value in Enum.GetValues(typeof(Resizer)))
            {
                VisualElement resizer = this.MandatoryQ(value.ToString().ToLower() + "-resize");
                resizer.AddManipulator(new ElementResizer(this, value));
            }

            foreach (Resizer vertical in new[] {Resizer.Top, Resizer.Bottom})
            {
                foreach (Resizer horizontal in new[] {Resizer.Left, Resizer.Right})
                {
                    VisualElement resizer =
                        this.MandatoryQ(vertical.ToString().ToLower() + "-" + horizontal.ToString().ToLower() + "-resize");
                    resizer.AddManipulator(new ElementResizer(this, vertical | horizontal));
                }
            }
        }

        [Flags]
        public enum Resizer
        {
            Top    = 1 << 0,
            Bottom = 1 << 1,
            Left   = 1 << 2,
            Right  = 1 << 3,
        }
    }
}
