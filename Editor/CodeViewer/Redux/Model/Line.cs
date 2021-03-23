using System;
using System.Collections.Generic;

namespace UnityEditor.CodeViewer
{
    public class Line : ILine
    {
        public int LineNumber { get; }
        public string Text { get; }
        public object Metadata { get; set; }
        public IReadOnlyList<ILineDecorator> Decorators => m_Decorators.AsReadOnly();

        readonly List<ILineDecorator> m_Decorators = new List<ILineDecorator>();

        public Line(int lineNumber, string text = "", object metadata = null, params ILineDecorator[] decorators)
        {
            LineNumber = lineNumber;
            Text = text;
            Metadata = metadata;
            m_Decorators.AddRange(decorators);
        }

        public void AddDecorator(ILineDecorator decorator)
        {
            m_Decorators.Add(decorator);
        }
    }
}
