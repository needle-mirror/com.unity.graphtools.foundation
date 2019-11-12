using System;
using UnityEngine.Assertions;

namespace UnityEditor.VisualScripting.Model
{
    [AttributeUsage(AttributeTargets.Field)]
    public class TypeSearcherAttribute : Attribute
    {
        public ISearcherFilter Filter { get; }

        public TypeSearcherAttribute() {}

        public TypeSearcherAttribute(Type filter)
        {
            Assert.IsTrue(typeof(ISearcherFilter).IsAssignableFrom(filter),
                "The filter is not type of ISearcherFilter");

            Filter = (ISearcherFilter)Activator.CreateInstance(filter);
        }
    }
}
