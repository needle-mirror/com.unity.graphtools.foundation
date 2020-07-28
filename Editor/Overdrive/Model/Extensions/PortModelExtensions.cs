using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Model
{
    public static class PortModelExtensions
    {
        public static bool IsConnected(this IGTFPortModel self) => self.GetConnectedEdges().Any();
    }
}
