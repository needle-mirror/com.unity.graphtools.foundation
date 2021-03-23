using System;
using JetBrains.Annotations;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model
{
    //[SearcherItem(typeof(ClassStencil), SearcherContext.Graph, "Events/On Key Down Event")]
    [Serializable]
    public class KeyDownEventModel : FunctionModel, IEventFunctionModel
    {
        public override bool IsInstanceMethod => true;

        [PublicAPI]
        public enum EventMode { Held, Pressed, Released }

        const string k_Title = "On Key Event";

        public override string Title => k_Title;
        public override bool AllowMultipleInstances => true;
        public override bool HasReturnType => false;

        public EventMode mode;

        public IPortModel KeyPort { get; private set; }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            KeyPort = AddDataInputPort<KeyCode>("key");
        }
    }
}
