#if UNITY_2020_1_OR_NEWER
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.GraphViewModel;

namespace UnityEditor.VisualScripting.Editor
{
    public class PlacematContainer : Experimental.GraphView.PlacematContainer
    {
        readonly Store m_Store;

        public PlacematContainer(Store store, GraphView gv)
            : base(gv)
        {
            m_Store = store;
        }

        protected override void UpdateElementsOrder()
        {
            var origOrders = Placemats.ToDictionary(pm => pm, pm => pm.ZOrder);
            base.UpdateElementsOrder();

            var changed = Placemats.Where(pm => origOrders[pm] != pm.ZOrder).OfType<Placemat>().ToList();
            m_Store.Dispatch(
                new ChangePlacematZOrdersAction(
                    changed.Select(pm => pm.ZOrder).ToArray(),
                    changed.Select(pm => pm.GraphElementModel).Cast<PlacematModel>().ToArray()));
        }
    }
}
#endif
