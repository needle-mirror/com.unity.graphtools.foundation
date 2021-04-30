using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class MathBook : GraphModel
    {
        static private readonly List<MathNode> s_EmptyNodes = new List<MathNode>();

        public MathBook()
        {
            StencilType = null;
        }

        public override Type DefaultStencilType => typeof(MathBookStencil);
    }
}
