using UnityEditor.Experimental.GraphView;

namespace UnityEditor.VisualScripting.Editor.SmartSearch
{
    public class SearcherGraphView : GraphView
    {
        public Store store { get; }

        public SearcherGraphView(Store store)
        {
            this.store = store;
        }
    }
}
