#if !UNITY_2019_3_OR_NEWER
using System;
using JetBrains.Annotations;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    // 2019.2 Doesn't support binding for Label
    // this class is basically Label from 2019.3
    public class BoundLabel : Label, IBindable, INotifyValueChanged<string>
    {
        [UsedImplicitly]
        public new class UxmlFactory : UxmlFactory<BoundLabel, UxmlTraits> {}

        public IBinding binding { get; set; }
        public string bindingPath { get; set; }

        public string value
        {
            get => text;
            set => text = value;
        }

        public void SetValueWithoutNotify(string newValue)
        {
            text = newValue;
        }
    }
}
#endif
