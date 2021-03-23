using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.CodeViewer
{
    public class Document : IDocument
    {
        public IReadOnlyList<ILine> Lines => m_Lines.AsReadOnly();
        public Action<object> Callback { get; set; }

        readonly List<ILine> m_Lines = new List<ILine>();

        public Document(Action<object> callback, params ILine[] lines)
        {
            Callback = callback;
            m_Lines.AddRange(lines);
        }

        public Document(params ILine[] lines)
        {
            m_Lines.AddRange(lines);
        }

        public void AddLine(ILine line)
        {
            m_Lines.Add(line);
        }

        public string FullText
        {
            get { return String.Join(Environment.NewLine, Lines.Select(l => l.Text)); }
        }
    }
}
