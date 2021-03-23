using System;
using UnityEngine;

namespace UnityEngine.VisualScripting
{
    [AttributeUsage(AttributeTargets.All)]
    public class VisualScriptingFriendlyNameAttribute : Attribute
    {
        public string FriendlyName { get; }

        public VisualScriptingFriendlyNameAttribute(string friendlyName)
        {
            FriendlyName = friendlyName;
        }
    }
}
