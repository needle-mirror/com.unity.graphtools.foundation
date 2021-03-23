using System;
using UnityEditor.VisualScripting.Model;

namespace UnityEditor.VisualScripting.Editor
{
    public class VSEditorPrefs : VSPreferences
    {
        const string k_VSEditorPrefPrefix = "VisualScripting.";

        public VSEditorPrefs()
        {
            InitDefaultForEditorPrefs();

            ReadAllFromEditorPrefs();
        }

        void InitDefaultForEditorPrefs()
        {
            // specific default values, if you want something else than VSPreferences default for editor
            if (Unsupported.IsDeveloperBuild())
            {
                base.SetBool(BoolPref.ErrorOnRecursiveDispatch, true);
                base.SetBool(BoolPref.ErrorOnMultipleDispatchesPerFrame, true);
            }
        }

        public override void SetBool(BoolPref k, bool value)
        {
            base.SetBool(k, value);
            EditorPrefs.SetBool(GetKeyName(k), value);
        }

        public override void SetInt(IntPref k, int value)
        {
            base.SetInt(k, value);
            EditorPrefs.SetInt(GetKeyName(k), value);
        }

        void ReadAllFromEditorPrefs()
        {
            foreach (BoolPref pref in GetEachKey<BoolPref>())
            {
                ReadBoolFromEditorPref(pref);
            }
            foreach (IntPref pref in GetEachKey<IntPref>())
            {
                ReadIntFromEditorPref(pref);
            }
        }

        void ReadBoolFromEditorPref(BoolPref k)
        {
            string keyName = GetKeyName(k);
            bool pref = GetBool(k);
            bool value = EditorPrefs.GetBool(keyName, pref);
            base.SetBool(k, value);
        }

        void ReadIntFromEditorPref(IntPref k)
        {
            string keyName = GetKeyName(k);
            int pref = GetInt(k);
            int value = EditorPrefs.GetInt(keyName, pref);
            base.SetInt(k, value);
        }

        static string GetKeyName<T>(T key)
        {
            return k_VSEditorPrefPrefix + key;
        }
    }
}
