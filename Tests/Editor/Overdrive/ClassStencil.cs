using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Compilation;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using PortInitializationTraversal = UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.PortInitializationTraversal;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.Model.Stencils", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class ClassStencil : Stencil
    {
        ISearcherFilterProvider m_SearcherFilterProvider;
        ISearcherDatabaseProvider m_SearcherDatabaseProvider;
        List<ITypeMetadata> m_AssembliesTypes;

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

        public override IBuilder Builder => null;

        public override void PreProcessGraph(IGTFGraphModel graphModel)
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

        public override List<ITypeMetadata> GetAssembliesTypesMetadata()
        {
            if (m_AssembliesTypes != null)
                return m_AssembliesTypes;

            var types = AssemblyCache.CachedAssemblies.SelectMany(a => a.GetTypesSafe()).ToList();
            m_AssembliesTypes = TaskUtility.RunTasks<Type, ITypeMetadata>(types, (type, cb) =>
            {
                if (IsValidType(type))
                    cb.Add(TypeSerializer.GenerateTypeHandle(type).GetMetadata(this));
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
