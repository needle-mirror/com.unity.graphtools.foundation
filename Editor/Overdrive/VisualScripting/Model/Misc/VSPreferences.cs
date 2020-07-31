using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public sealed class VSBoolPref : BoolPref
    {
        VSBoolPref(int id, string name)
            : base(id, name)
        {
        }
    }

    public sealed class VSIntPref : IntPref
    {
        public static readonly VSIntPref ItemizeOptions = new VSIntPref(k_ToolBasePrefId, nameof(ItemizeOptions));

        VSIntPref(int id, string name)
            : base(id, name)
        {
        }
    }

    public sealed class VSPreferences : Preferences
    {
        public static VSPreferences CreatePreferences()
        {
            var preferences = new VSPreferences();
            preferences.Initialize<VSBoolPref, VSIntPref>();
            return preferences;
        }

        VSPreferences() {}

        const string k_EditorPrefPrefix = "VisualScripting.";
        protected override string GetEditorPreferencesPrefix()
        {
            return k_EditorPrefPrefix;
        }

        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();
            SetIntNoEditorUpdate(VSIntPref.ItemizeOptions, (int)ItemizeOptions.Default);
        }

        [Flags]
        public enum ItemizeOptions
        {
            Nothing = 0,
            Variables = 1,
            Constants = 2,
            SystemConstants = 4,

            All = Variables | Constants | SystemConstants,
            Default = Variables | SystemConstants
        }

        public ItemizeOptions CurrentItemizeOptions
        {
            get => (ItemizeOptions)GetInt(VSIntPref.ItemizeOptions);
            set => SetInt(VSIntPref.ItemizeOptions, (int)value);
        }

        public void ToggleItemizeOption(ItemizeOptions op)
        {
            CurrentItemizeOptions ^= op;
        }
    }
}
