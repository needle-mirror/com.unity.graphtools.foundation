using JetBrains.Annotations;
using UnityEditor.Experimental.GraphView;

namespace UnityEditor.VisualScripting.Editor.Plugins
{
    public interface IPluginHandler
    {
        void Register(Store store, GraphView graphView);
        void Unregister();

        void OptionsMenu(GenericMenu menu);
    }
}
