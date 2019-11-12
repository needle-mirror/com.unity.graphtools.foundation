using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;

namespace UnityEditor.VisualScripting.Editor.SmartSearch
{
    public static class SearcherItemCollectionUtility
    {
        public static bool IsMethodBlackListed([NotNull] MethodBase methodBase,
            Dictionary<string, List<Type>> blackList)
        {
            if (blackList == null)
            {
                return false;
            }

            foreach (KeyValuePair<string, List<Type>> method in blackList)
            {
                if (!methodBase.Name.StartsWith(method.Key, StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (Type type in method.Value)
                {
                    if (methodBase.DeclaringType == type)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
