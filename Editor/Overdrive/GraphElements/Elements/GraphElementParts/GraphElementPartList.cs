using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class GraphElementPartList : IEnumerable<IGraphElementPart>
    {
        List<IGraphElementPart> m_Parts = new List<IGraphElementPart>();

        public void AppendPart(IGraphElementPart child)
        {
            if (child != null)
                m_Parts.Add(child);
        }

        public IGraphElementPart GetPart(string name)
        {
            var index = m_Parts.FindIndex(e => e.PartName == name);
            return index == -1 ? null : m_Parts[index];
        }

        public void InsertPartBefore(string beforeChild, IGraphElementPart child)
        {
            if (child != null)
            {
                var index = m_Parts.FindIndex(e => e.PartName == beforeChild);
                if (index != -1)
                {
                    m_Parts.Insert(index, child);
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(beforeChild), beforeChild, "Part not found");
                }
            }
        }

        public void InsertPartAfter(string afterChild, IGraphElementPart child)
        {
            if (child != null)
            {
                var index = m_Parts.FindIndex(e => e.PartName == afterChild);
                if (index != -1)
                {
                    m_Parts.Insert(index + 1, child);
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(afterChild), afterChild, "Part not found");
                }
            }
        }

        public void ReplacePart(string componentToReplace, IGraphElementPart child)
        {
            if (child != null)
            {
                var index = m_Parts.FindIndex(e => e.PartName == componentToReplace);
                if (index != -1)
                {
                    m_Parts.RemoveAt(index);
                    m_Parts.Insert(index, child);
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(componentToReplace), componentToReplace, "Part not found");
                }
            }
        }

        public void RemovePart(string name)
        {
            var index = m_Parts.FindIndex(e => e.PartName == name);
            if (index != -1)
            {
                m_Parts.RemoveAt(index);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(name), name, "Part not found");
            }
        }

        public IEnumerator<IGraphElementPart> GetEnumerator()
        {
            return m_Parts.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
