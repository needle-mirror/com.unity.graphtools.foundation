using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace UnityEditor.VisualScripting.Model
{
    [PublicAPI]
    public class VSPreferences
    {
        public enum BoolPref
        {
            FullUIRebuildOnChange,
            WarnOnUIFullRebuild,
            LogUIBuildTime,
            BoundObjectLogging,
            AutoRecompile,
            AutoAlignDraggedEdges,
            DependenciesLogging,
            ErrorOnRecursiveDispatch,
            ErrorOnMultipleDispatchesPerFrame,
            LogAllDispatchedActions,
            ShowUnusedNodes,
        }

        public enum IntPref
        {
        }

        Dictionary<BoolPref, bool> m_BoolPrefs;
        Dictionary<IntPref, int> m_IntPrefs;

        public VSPreferences()
        {
            ResetToDefaults();
        }

        void ResetToDefaults()
        {
            InitPrefType(ref m_BoolPrefs);
            InitPrefType(ref m_IntPrefs);

            // specific default values, if you want something else than default(type)
            m_BoolPrefs[BoolPref.AutoRecompile] = true;
            m_BoolPrefs[BoolPref.AutoAlignDraggedEdges] = true;
        }

        public bool GetBool(BoolPref k)
        {
            return m_BoolPrefs[k];
        }

        public int GetInt(IntPref k)
        {
            return m_IntPrefs[k];
        }

        public virtual void SetBool(BoolPref k, bool value)
        {
            m_BoolPrefs[k] = value;
        }

        public virtual void SetInt(IntPref k, int value)
        {
            m_IntPrefs[k] = value;
        }

        public void ToggleBool(BoolPref k)
        {
            SetBool(k, !GetBool(k));
        }

        public static IEnumerable<TKey> GetEachKey<TKey>()
        {
            return Enum.GetValues(typeof(TKey)).Cast<TKey>();
        }

        static void InitPrefType<TKey, TValue>(ref Dictionary<TKey, TValue> prefs)
        {
            var keys = GetEachKey<TKey>();
            if (prefs == null)
                prefs = new Dictionary<TKey, TValue>();
            foreach (TKey key in keys)
                prefs[key] = default;
        }

        public Type PluginTypePref;
    }
}
