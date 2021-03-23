using System;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine.Assertions;

namespace UnityEditor.VisualScripting.Editor.SmartSearch
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SearcherItemAttribute : Attribute
    {
        public Type StencilType { get; }
        public string Path { get; }
        public SearcherContext Context { get; }

        public SearcherItemAttribute(Type stencilType, SearcherContext context, string path)
        {
            Assert.IsTrue(
                stencilType.IsSubclassOf(typeof(Stencil)),
                $"Parameter stencilType is type of {stencilType} which is not a subclass of UnityEditor.VisualScripting.Model.Stencils.Stencil");

            StencilType = stencilType;
            Path = path;
            Context = context;
        }
    }
}
