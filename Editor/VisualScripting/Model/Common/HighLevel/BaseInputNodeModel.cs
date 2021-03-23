using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Translators;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model
{
    [Serializable]
    public abstract class BaseInputNodeModel : HighLevelNodeModel
    {
        [PublicAPI]
        public KeyDownEventModel.EventMode Mode;
        protected IPortModel InputPort { get; set; }
        protected IPortModel ButtonOutputPort { get; set; }
        protected abstract string MethodName(IPortModel portModel);
    }

    [GraphtoolsExtensionMethods]
    public static class BaseInputNodeTranslator
    {
    }
}
