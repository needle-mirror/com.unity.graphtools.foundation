using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IHasTitle
    {
        string Title { get; set; }
        string DisplayTitle { get; }
    }

    public interface IHasProgress
    {
        bool HasProgress { get; }
    }

    // TODO Consider moving this functionality to GraphElement since we have capabilities to gate the action.
    public interface ICollapsible
    {
        bool Collapsed { get; set; }
    }

    // TODO Consider moving this functionality to GraphElement since we have capabilities to gate the action.
    public interface IResizable
    {
        Rect PositionAndSize { get; set; }
    }

    // TODO Consider moving this functionality to GraphElement since we have capabilities to gate the action.
    public interface IMovable
    {
        Vector2 Position { get; set; }
        void Move(Vector2 delta);
    }

    // TODO Consider moving this functionality to GraphElement since we have capabilities to gate the action.
    public interface IRenamable
    {
        void Rename(string newName);
    }

    public interface IGhostEdge
    {
        Vector2 EndPoint { get; }
    }
}
