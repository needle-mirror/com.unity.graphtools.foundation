using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor.Searcher;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor.SmartSearch
{
    [PublicAPI]
    public class TypeSearcherDatabase
    {
        readonly List<ITypeMetadata> m_TypesMetadata;
        public Stencil Stencil { get; }

        List<Action<List<SearcherItem>>> m_Registrations;
        List<Func<List<SearcherItem>, ITypeMetadata, bool>> m_MetaRegistrations;


        public TypeSearcherDatabase(Stencil stencil, List<ITypeMetadata> typesMetadata)
        {
            Stencil = stencil;
            m_TypesMetadata = typesMetadata;
            m_Registrations = new List<Action<List<SearcherItem>>>();
            m_MetaRegistrations = new List<Func<List<SearcherItem>, ITypeMetadata, bool>>();
        }

        public void RegisterTypesFromMetadata(Func<List<SearcherItem>, ITypeMetadata, bool> register)
        {
            m_MetaRegistrations.Add(register);
        }

        public void RegisterTypes(Action<List<SearcherItem>> register)
        {
            m_Registrations.Add(register);
        }

        public virtual TypeSearcherDatabase AddClasses()
        {
            RegisterTypesFromMetadata((items, metadata) =>
            {
                var classItem = new TypeSearcherItem(metadata.TypeHandle, metadata.FriendlyName);
                return items.TryAddClassItem(Stencil, classItem, metadata);
            });
            return this;
        }

        public virtual TypeSearcherDatabase AddEnums()
        {
            RegisterTypesFromMetadata((items, metadata) =>
            {
                var enumItem = new TypeSearcherItem(metadata.TypeHandle, metadata.FriendlyName);
                return items.TryAddEnumItem(enumItem, metadata);
            });
            return this;
        }

        public SearcherDatabase Build()
        {
            var items = new List<SearcherItem>();

            foreach (var meta in m_TypesMetadata)
            {
                foreach (var metaRegistration in m_MetaRegistrations)
                {
                    if (metaRegistration.Invoke(items, meta))
                        break;
                }
            }

            foreach (var registration in m_Registrations)
            {
                registration.Invoke(items);
            }

            return SearcherDatabase.Create(items, "", false);
        }
    }
}
