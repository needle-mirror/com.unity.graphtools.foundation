using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;

namespace UnityEditor.VisualScripting.Model
{
    [Serializable]
    public class TagConstantModel : ConstantNodeModel<TagName>, IStringWrapperConstantModel
    {
        public string Label => "Tag";

        public List<string> GetAllInputNames()
        {
            return InternalEditorUtility.tags.ToList();
        }

        public string StringValue
        {
            get => value.name;
            set => this.value.name = value;
        }
    }

    [Serializable]
    public struct TagName
    {
        public string name;

        public override string ToString()
        {
            return name ?? "";
        }
    }
}
