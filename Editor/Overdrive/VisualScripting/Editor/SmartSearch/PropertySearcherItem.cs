using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEditor.Searcher;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch
{
    public class PropertySearcherItem : SearcherItem
    {
        public bool Enabled;
        public PropertyElement Element;

        int m_HashCode;

        public MemberInfoValue MemberInfo { get; }

        public PropertySearcherItem(MemberInfoValue memberInfo, string path, int hashcode, string help = "", List<SearcherItem> children = null)
            : base(memberInfo.Name, help, children)
        {
            MemberInfo = memberInfo;
            m_HashCode = hashcode;
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode() => m_HashCode;
    }

    // NOTE: it HAS to inherit from TemplateContainer as the expander toggle handler
    // uses UQuery to find it and and the matching SearcherItem stored in its userdata
    public class PropertyElement : TemplateContainer
    {
        public Toggle Toggle;
        public PropertySearcherItem Item { get; set; }

        public PropertyElement(string templateId)
            : base(templateId) {}
    }
}
