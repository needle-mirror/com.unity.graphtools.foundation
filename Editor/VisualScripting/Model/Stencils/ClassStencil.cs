using System;
using System.Collections.Generic;
using System.Linq;
using Packages.VisualScripting.Editor.Helpers;
using UnityEditor.EditorCommon.Extensions;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.Model.Compilation;
using UnityEngine;
using UnityEngine.VisualScripting;

namespace UnityEditor.VisualScripting.Model.Stencils
{
    public class ClassStencil : Stencil
    {
        ISearcherFilterProvider m_SearcherFilterProvider;
        ISearcherDatabaseProvider m_SearcherDatabaseProvider;
        List<ITypeMetadata> m_AssembliesTypes;
        IRuntimeStencilReference m_RuntimeReference;

        static readonly string[] k_BlackListedNamespaces =
        {
            "aot",
            "collabproxy",
            "icsharpcode",
            "jetbrains",
            "microsoft",
            "mono",
            "packages.visualscripting",
            "treeeditor",
            "unityeditorinternal",
            "unityengineinternal",
            "visualscripting"
        };

        public override StencilCapabilityFlags Capabilities => StencilCapabilityFlags.SupportsMacros;
        public override IBuilder Builder => RoslynCompiler.DefaultBuilder;
        public override void PreProcessGraph(VSGraphModel graphModel)
        {
            new PortInitializationTraversal().VisitGraph(graphModel);
        }

        public override ISearcherFilterProvider GetSearcherFilterProvider()
        {
            return m_SearcherFilterProvider ?? (m_SearcherFilterProvider = new ClassSearcherFilterProvider(this));
        }

        public override ISearcherDatabaseProvider GetSearcherDatabaseProvider()
        {
            return m_SearcherDatabaseProvider ?? (m_SearcherDatabaseProvider = new ClassSearcherDatabaseProvider(this));
        }

        public override IRuntimeStencilReference RuntimeReference =>
            m_RuntimeReference ?? (m_RuntimeReference = new RuntimeClassStencilReference());

        public override List<ITypeMetadata> GetAssembliesTypesMetadata()
        {
            if (m_AssembliesTypes != null)
                return m_AssembliesTypes;

            var types = GetAssemblies().SelectMany(a => a.GetTypesSafe()).ToList();
            m_AssembliesTypes = TaskUtility.RunTasks<Type, ITypeMetadata>(types, (type, cb) =>
            {
                if (IsValidType(type))
                    cb.Add(GenerateTypeHandle(type).GetMetadata(this));
            }).ToList();
            m_AssembliesTypes.Sort((x, y) => string.CompareOrdinal(
                x.TypeHandle.Identification,
                y.TypeHandle.Identification)
            );

            return m_AssembliesTypes;
        }

        static bool IsValidType(Type type)
        {
            return !type.IsAbstract
                && !type.IsInterface
                && type.IsVisible
                && !Attribute.IsDefined(type, typeof(ObsoleteAttribute))
                && !k_BlackListedNamespaces.Any(b => type.Namespace != null && type.Namespace.ToLower().StartsWith(b)
                && !Attribute.IsDefined(type, typeof(ObsoleteAttribute)));
        }
    }
}
