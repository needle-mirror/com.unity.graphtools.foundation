using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    [PublicAPI]
    class Scope : Experimental.GraphView.Scope
    {
        public Scope()
        {
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "Scope.uss"));

            style.overflow = Overflow.Visible;
            pickingMode = PickingMode.Ignore;
            capabilities = 0;
        }
    }
}
