using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEditor.VisualScripting.Model.Translators;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model
{
    [SearcherItem(typeof(ClassStencil), SearcherContext.Stack, "Debug/" + NodeTitle)]
    [Serializable]
    public class LogNodeModel : HighLevelNodeModel, IHasMainInputPort
    {
        public const string NodeTitle = "Log";

        public enum LogTypes { Message, Warning, Error }

        public LogTypes LogType = LogTypes.Message;

        public IPortModel InputPort { get; private set; }

        protected override void OnDefineNode()
        {
            InputPort = AddDataInputPort<object>("Object");
        }
    }
}
