using System.Collections.Generic;
using JetBrains.Annotations;

// ReSharper disable InconsistentNaming

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class BoolPref : Enumeration
    {
        public static readonly BoolPref FullUIRebuildOnChange = new BoolPref(0, nameof(FullUIRebuildOnChange));
        public static readonly BoolPref WarnOnUIFullRebuild = new BoolPref(1, nameof(WarnOnUIFullRebuild));
        public static readonly BoolPref LogUIBuildTime = new BoolPref(2, nameof(LogUIBuildTime));
        // 3 was BoundObjectLogging, now unused
        public static readonly BoolPref AutoRecompile = new BoolPref(4, nameof(AutoRecompile));
        public static readonly BoolPref AutoAlignDraggedEdges = new BoolPref(5, nameof(AutoAlignDraggedEdges));
        public static readonly BoolPref DependenciesLogging = new BoolPref(6, nameof(DependenciesLogging));
        public static readonly BoolPref ErrorOnRecursiveDispatch = new BoolPref(7, nameof(ErrorOnRecursiveDispatch));
        public static readonly BoolPref ErrorOnMultipleDispatchesPerFrame = new BoolPref(8, nameof(ErrorOnMultipleDispatchesPerFrame));
        public static readonly BoolPref LogAllDispatchedActions = new BoolPref(9, nameof(LogAllDispatchedActions));
        public static readonly BoolPref ShowUnusedNodes = new BoolPref(10, nameof(ShowUnusedNodes));
        public static readonly BoolPref SearcherInRegularWindow = new BoolPref(11, nameof(SearcherInRegularWindow));

        [PublicAPI]
        protected static readonly int k_ToolBasePrefId = 10000;

        protected BoolPref(int id, string name)
            : base(id, name)
        {
        }
    }

    public class IntPref : Enumeration
    {
        [PublicAPI]
        protected static readonly int k_ToolBasePrefId = 10000;

        protected IntPref(int id, string name)
            : base(id, name)
        {
        }
    }

    public abstract class Preferences
    {
        Dictionary<BoolPref, bool> m_BoolPrefs;
        Dictionary<IntPref, int> m_IntPrefs;

        protected Preferences()
        {
            m_BoolPrefs = new Dictionary<BoolPref, bool>();
            m_IntPrefs = new Dictionary<IntPref, int>();
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

        protected abstract string GetEditorPreferencesPrefix();

        protected virtual void SetDefaultValues()
        {
            // specific default values, if you want something else than default(type)
            SetBoolNoEditorUpdate(BoolPref.AutoRecompile, true);
            SetBoolNoEditorUpdate(BoolPref.AutoAlignDraggedEdges, true);

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
            return m_BoolPrefs[k];
        }

        public int GetInt(IntPref k)
        {
            return m_IntPrefs[k];
        }

        public void SetBool(BoolPref k, bool value)
        {
            SetBoolNoEditorUpdate(k, value);
            EditorPrefs.SetBool(GetKeyName(k), value);
        }

        public void SetInt(IntPref k, int value)
        {
            SetIntNoEditorUpdate(k, value);
            EditorPrefs.SetInt(GetKeyName(k), value);
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
            string keyName = GetKeyName(k);
            bool pref = GetBool(k);
            bool value = EditorPrefs.GetBool(keyName, pref);
            SetBoolNoEditorUpdate(k, value);
        }

        void ReadIntFromEditorPref(IntPref k)
        {
            string keyName = GetKeyName(k);
            int pref = GetInt(k);
            int value = EditorPrefs.GetInt(keyName, pref);
            SetIntNoEditorUpdate(k, value);
        }

        string GetKeyName<T>(T key)
        {
            return GetEditorPreferencesPrefix() + key;
        }
    }
}
