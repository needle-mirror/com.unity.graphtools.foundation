namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public interface IMovableGraphElement
    {
        void UpdatePinning();
        bool IsMovable { get; }
    }
}
