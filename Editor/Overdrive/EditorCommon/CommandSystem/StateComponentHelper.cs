using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    static class StateComponentHelper
    {
        internal static string Serialize(IStateComponent obj)
        {
            if (obj == null)
                return "";

            obj.BeforeSerialize();
            return JsonUtility.ToJson(obj);
        }

        internal static T Deserialize<T>(string jsonString) where T : IStateComponent
        {
            var obj = JsonUtility.FromJson<T>(jsonString);
            obj.AfterDeserialize();
            return obj;
        }
    }
}
