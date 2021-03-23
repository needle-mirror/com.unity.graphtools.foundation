using System;
using UnityEditor.VisualScripting.Model;

namespace UnityEditor.VisualScriptingTests.Actions
{
    [Serializable]
    class UniqueInstanceFunctionModel : FunctionModel
    {
        public override bool HasReturnType => false;

        public override bool AllowMultipleInstances => false;
        public override bool IsInstanceMethod => true;
    }
}
