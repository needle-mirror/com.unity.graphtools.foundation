using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.GraphToolsFoundation.Overdrive;

// ReSharper disable InconsistentNaming

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class BoolPref : Enumeration
    {
        // 0 was FullUIRebuildOnChange, now unused
        public static readonly BoolPref WarnOnUIFullRebuild = new BoolPref(1, nameof(WarnOnUIFullRebuild));
        public static readonly BoolPref LogUIBuildTime = new BoolPref(2, nameof(LogUIBuildTime));
        // 3 was BoundObjectLogging, now unused
        public static readonly BoolPref AutoProcess = new BoolPref(4, nameof(AutoProcess), new[] { "AutoRecompile" });
        public static readonly BoolPref AutoAlignDraggedEdges = new BoolPref(5, nameof(AutoAlignDraggedEdges));
        public static readonly BoolPref DependenciesLogging = new BoolPref(6, nameof(DependenciesLogging));
        public static readonly BoolPref ErrorOnRecursiveDispatch = new BoolPref(7, nameof(ErrorOnRecursiveDispatch));
        public static readonly BoolPref ErrorOnMultipleDispatchesPerFrame = new BoolPref(8, nameof(ErrorOnMultipleDispatchesPerFrame));
        public static readonly BoolPref LogAllDispatchedCommands = new BoolPref(9, nameof(LogAllDispatchedCommands), new[] { "LogAllDispatchedActions" });
        public static readonly BoolPref ShowUnusedNodes = new BoolPref(10, nameof(ShowUnusedNodes));
        public static readonly BoolPref SearcherInRegularWindow = new BoolPref(11, nameof(SearcherInRegularWindow));
        public static readonly BoolPref LogUIUpdate = new BoolPref(12, nameof(LogUIUpdate));

        [PublicAPI]
        protected static readonly int k_ToolBasePrefId = 10000;

        protected BoolPref(int id, string name, string[] obsoleteNames = null)
            : base(id, name, obsoleteNames)
        {
        }
    }

    public class IntPref : Enumeration
    {
        [PublicAPI]
        protected static readonly int k_ToolBasePrefId = 10000;

        protected IntPref(int id, string name, string[] obsoleteNames = null)
            : base(id, name, obsoleteNames)
        {
        }
    }

    public class Preferences
    {
        public static Preferences CreatePreferences(string editorPreferencesPrefix)
        {
            var preferences = new Preferences(editorPreferencesPrefix);
            preferences.Initialize<BoolPref, IntPref>();
            return preferences;
        }

        Dictionary<BoolPref, bool> m_BoolPrefs;
        Dictionary<IntPref, int> m_IntPrefs;
        string m_EditorPreferencesPrefix;

        Preferences()
        {
            m_BoolPrefs = new Dictionary<BoolPref, bool>();
            m_IntPrefs = new Dictionary<IntPref, int>();
        }

        protected Preferences(string editorPreferencesPrefix) : this()
        {
            m_EditorPreferencesPrefix = editorPreferencesPrefix;
        }

        protected void Initialize<TBool, TInt>()
            where TBool : BoolPref
            where TInt : IntPref
        {
            InitPrefType<TBool, BoolPref, bool>(m_BoolPrefs);
            InitPrefType<TInt, IntPref, int>(m_IntPrefs);

            SetDefaultValues();
            ReadAllFromEditorPrefs<TBool, TInt>();
        }

        protected virtual void SetDefaultValues()
        {
            // specific default values, if you want something else than default(type)
            if (Unsupported.IsDeveloperBuild())
            {
                SetBoolNoEditorUpdate(BoolPref.ErrorOnRecursiveDispatch, true);
                SetBoolNoEditorUpdate(BoolPref.ErrorOnMultipleDispatchesPerFrame, true);
            }
        }

        public void SetBoolNoEditorUpdate(BoolPref k, bool value)
        {
            m_BoolPrefs[k] = value;
        }

        public void SetIntNoEditorUpdate(IntPref k, int value)
        {
            m_IntPrefs[k] = value;
        }

        public bool GetBool(BoolPref k)
        {
            m_BoolPrefs.TryGetValue(k, out var result);
            return result;
        }

        public int GetInt(IntPref k)
        {
            m_IntPrefs.TryGetValue(k, out var result);
            return result;
        }

        public void SetBool(BoolPref k, bool value)
        {
            SetBoolNoEditorUpdate(k, value);
            EditorPrefs.SetBool(GetKeyName(k), value);

            if (k.ObsoleteNames != null)
            {
                foreach (var obsoleteName in k.ObsoleteNames)
                {
                    EditorPrefs.DeleteKey(m_EditorPreferencesPrefix + obsoleteName);
                }
            }
        }

        public void SetInt(IntPref k, int value)
        {
            SetIntNoEditorUpdate(k, value);
            EditorPrefs.SetInt(GetKeyName(k), value);

            if (k.ObsoleteNames != null)
            {
                foreach (var obsoleteName in k.ObsoleteNames)
                {
                    EditorPrefs.DeleteKey(m_EditorPreferencesPrefix + obsoleteName);
                }
            }
        }

        public void ToggleBool(BoolPref k)
        {
            SetBool(k, !GetBool(k));
        }

        static void InitPrefType<TKeySource, TKey, TValue>(Dictionary<TKey, TValue> prefs)
            where TKeySource : TKey
            where TKey : Enumeration
        {
            var keys = Enumeration.GetAll<TKeySource, TKey>();
            foreach (var key in keys)
                prefs[key] = default;
        }

        void ReadAllFromEditorPrefs<TBool, TInt>()
            where TBool : BoolPref
            where TInt : IntPref
        {
            foreach (var pref in Enumeration.GetAll<TBool, BoolPref>())
            {
                ReadBoolFromEditorPref(pref);
            }
            foreach (var pref in Enumeration.GetAll<TInt, IntPref>())
            {
                ReadIntFromEditorPref(pref);
            }
        }

        void ReadBoolFromEditorPref(BoolPref k)
        {
            string keyName = GetKeyNameInEditorPrefs(k);
            if (keyName != null)
            {
                bool value = EditorPrefs.GetBool(keyName);
                SetBoolNoEditorUpdate(k, value);
            }
        }

        void ReadIntFromEditorPref(IntPref k)
        {
            string keyName = GetKeyNameInEditorPrefs(k);
            if (keyName != null)
            {
                int value = EditorPrefs.GetInt(keyName);
                SetIntNoEditorUpdate(k, value);
            }
        }

        string GetKeyName<T>(T key) where T : Enumeration
        {
            return m_EditorPreferencesPrefix + key;
        }

        IEnumerable<string> GetObsoleteKeyNames<T>(T key) where T : Enumeration
        {
            foreach (var obsoleteName in key.ObsoleteNames)
            {
                yield return m_EditorPreferencesPrefix + obsoleteName;
            }
        }

        string GetKeyNameInEditorPrefs<T>(T key) where T : Enumeration
        {
            var keyName = GetKeyName(key);

            if (!EditorPrefs.HasKey(keyName) && key.ObsoleteNames != null)
            {
                keyName = null;

                foreach (var obsoleteKeyName in GetObsoleteKeyNames(key))
                {
                    if (EditorPrefs.HasKey(obsoleteKeyName))
                    {
                        keyName = obsoleteKeyName;
                        break;
                    }
                }
            }

            return keyName;
        }
    }
}
