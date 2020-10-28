using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public static class PortModelExtensions
    {
        public static bool IsConnected(this IPortModel self) => self.GetConnectedEdges().Any();
    }
}
