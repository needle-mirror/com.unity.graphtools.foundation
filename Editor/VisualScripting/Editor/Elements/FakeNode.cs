using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    [UsedImplicitly]
    class FakeNode : VisualElement
    {
        public FakeNode(string fakeText)
        {
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "Node.uss"));
            Add(new Label(fakeText) { name = "fakeText" });
        }
    }
}
