using System;
using JetBrains.Annotations;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model
{
    [PublicAPI]
    [Serializable]
    public abstract class EventFunctionModel : FunctionModel, IEventFunctionModel
    {
        public override bool IsInstanceMethod => true;
        public override bool HasReturnType => false;

        public override bool AllowMultipleInstances => false;

        public override string IconTypeString => "typeEventFunction";
    }
}
