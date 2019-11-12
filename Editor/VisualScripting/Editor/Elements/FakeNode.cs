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
            // @TODO: This might need to be reviewed in favor of a better / more scalable approach (non preprocessor based)
            // that would ideally bring the same level of backward/forward compatibility and/or removed when a 2013 beta version lands.
#if UNITY_2019_3_OR_NEWER
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "Node.2019.3.uss"));
#endif
            Add(new Label(fakeText) { name = "fakeText" });
        }
    }
}
