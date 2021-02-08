namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IPluginHandler
    {
        void Register(GraphViewEditorWindow window);
        void Unregister();

        void OptionsMenu(GenericMenu menu);
    }
}
