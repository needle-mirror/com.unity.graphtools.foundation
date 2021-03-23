using System;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    public interface IRenamableModel : IGraphElementModel
    {
        void Rename(string newName);
    }
}
