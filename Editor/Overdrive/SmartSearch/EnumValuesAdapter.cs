using System;
using UnityEditor.Searcher;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class EnumValuesAdapter : SimpleSearcherAdapter
    {
        public class EnumValueSearcherItem : SearcherItem
        {
            public EnumValueSearcherItem(Enum value, string help = "")
                : base(value.ToString(), help)
            {
                this.value = value;
            }

            public readonly Enum value;
        }

        public EnumValuesAdapter(string title)
            : base(title) {}
    }
}
