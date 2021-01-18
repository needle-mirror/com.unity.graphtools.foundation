using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public static class PortModelExtensions
    {
        public static bool IsConnected(this IPortModel self) => self.GetConnectedEdges().Any();

        public static bool Equivalent(this IPortModel a, IPortModel b)
        {
            if (a == null || b == null)
                return a == b;

            return a.Direction == b.Direction && a.NodeModel.Guid == b.NodeModel.Guid && a.UniqueName == b.UniqueName;
        }
    }
}
