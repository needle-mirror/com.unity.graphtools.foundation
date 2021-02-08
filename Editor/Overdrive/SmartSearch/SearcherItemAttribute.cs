using System;
using UnityEngine.Assertions;

namespace UnityEditor.GraphToolsFoundation.Overdrive
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
                $"Parameter stencilType is type of {stencilType.FullName} which is not a subclass of {typeof(Stencil).FullName}");

            StencilType = stencilType;
            Path = path;
            Context = context;
        }
    }
}
