namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Plugins
{
    public interface IPluginHandler
    {
        void Register(Store store, VseWindow window);
        void Unregister();

        void OptionsMenu(GenericMenu menu);
    }
}
