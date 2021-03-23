using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Searcher;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;

namespace UnityEditor.VisualScripting.Editor.SmartSearch
{
    public struct PropertySearcherItemsBuilder
    {
        readonly int m_MaxRecursiveDepth;
        readonly TypeHandle m_RootTypeHandle;
        readonly ITypeMetadataResolver m_Resolver;
        readonly BindingFlags m_RootFlags;
        readonly BindingFlags m_RecursiveFlags;
        readonly HashSet<int> m_ExistingMembers;

        public PropertySearcherItemsBuilder(int maxRecursiveDepth, TypeHandle rootTypeHandle,
                                            ITypeMetadataResolver resolver, HashSet<int> existingMembers)
        {
            m_MaxRecursiveDepth = maxRecursiveDepth;
            m_RootTypeHandle = rootTypeHandle;
            m_Resolver = resolver;
            m_RecursiveFlags = BindingFlags.Public | BindingFlags.Instance;
            m_RootFlags = BindingFlags.NonPublic | m_RecursiveFlags;
            m_ExistingMembers = existingMembers;
        }

        public List<SearcherItem> Build()
        {
            ITypeMetadata memberTypeMetadata = m_Resolver.Resolve(m_RootTypeHandle);
            return SearcherItemsForType(m_MaxRecursiveDepth, memberTypeMetadata, "", 0, m_RootFlags);
        }

        List<SearcherItem> SearcherItemsForType(int recursiveDepth, ITypeMetadata currentType, string currentPath,
            int parentsHashCode, BindingFlags flags)
        {
            List<SearcherItem> searcherItems = null;
            foreach (var member in currentType.GetMembers(flags))
            {
                var childItem = SearcherItemForType(recursiveDepth, currentPath, parentsHashCode, member);

                searcherItems = searcherItems ?? new List<SearcherItem>();
                searcherItems.Add(childItem);
            }

            return searcherItems;
        }

        PropertySearcherItem SearcherItemForType(int depth, string path, int parentHash, MemberInfoValue member)
        {
            TypeHandle memberType = member.UnderlyingType;
            ITypeMetadata memberTypeMetadataCSharp = m_Resolver.Resolve(memberType);
            string memberName = member.Name;
            int hashCode = GenerateSearcherItemHashCode(parentHash, memberName);

            List<SearcherItem> childItems = null;
            if (depth > 0)
                childItems = SearcherItemsForType(depth - 1, memberTypeMetadataCSharp, path + " " + memberName, hashCode, m_RecursiveFlags);

            return CreateSearcherItem(path, member, hashCode, childItems);
        }

        PropertySearcherItem CreateSearcherItem(string path, MemberInfoValue member, int hashCode,
            List<SearcherItem> childItems)
        {
            var childItem = new PropertySearcherItem(member, path, hashCode, children: childItems);
            childItem.Enabled = m_ExistingMembers.Contains(childItem.GetHashCode());

            return childItem;
        }

        static int GenerateSearcherItemHashCode(int parentsHashCode, string memberName)
        {
            unchecked
            {
                return (parentsHashCode * 397) ^ memberName.GetHashCode();
            }
        }
    }
}
