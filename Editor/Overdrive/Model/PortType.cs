using JetBrains.Annotations;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class PortType : Enumeration
    {
        public static readonly PortType Execution = new PortType(0, nameof(Execution));
        public static readonly PortType Data = new PortType(1, nameof(Data));
        public static readonly PortType MissingPort = new PortType(2, nameof(MissingPort));

        [PublicAPI]
        // If adding new PortType, use this value as the base id.
        protected static readonly int k_ToolBasePortTypeId = 1000;

        protected PortType(int id, string name)
            : base(id, name)
        {
        }
    }
}
