namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IPluginHandler
    {
        void Register(Store store, GraphViewEditorWindow window);
        void Unregister();

        void OptionsMenu(GenericMenu menu);
    }
}
