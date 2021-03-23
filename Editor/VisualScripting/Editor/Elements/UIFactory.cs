using System;
using JetBrains.Annotations;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    [PublicAPI]
    interface IUIFactory
    {
        Type CreatesFromType { get; }
        VisualElement Create(object model, Store store);
    }

    [UsedImplicitly]
    abstract class UIFactory<T> : IUIFactory where T : class
    {
        public Type CreatesFromType => typeof(T);

        public VisualElement Create(object model, Store store)
        {
            return DoCreate(model, store);
        }

        protected abstract VisualElement DoCreate(object model, Store store);
    }
}
