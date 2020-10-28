using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace  UnityEditor.GraphToolsFoundation.Overdrive
{
    public class CompilerQuickFix
    {
        public string description;
        public Action<Store> quickFix;

        public CompilerQuickFix(string description, Action<Store> quickFix)
        {
            this.description = description;
            this.quickFix = quickFix;
        }
    }

    public class CompilerError
    {
        public string description;
        public INodeModel sourceNode;
        public GUID sourceNodeGuid;
        public CompilerQuickFix quickFix;
        public bool isWarning;

        public override string ToString()
        {
            return $"Compiler error: {description}";
        }
    }

    public class CompilationResult
    {
        public string[] sourceCode;
        public Dictionary<Type, string> pluginSourceCode;
        public List<CompilerError> errors = new List<CompilerError>();

        public CompilationStatus status => errors.Any(e => e.isWarning == false) ?
        CompilationStatus.Failed : CompilationStatus.Succeeded;

        public void AddError(string description, INodeModel node = null, CompilerQuickFix quickFix = null)
        {
            AddError(description, node,  false, quickFix);
        }

        public void AddWarning(string description, INodeModel node = null, CompilerQuickFix quickFix = null)
        {
            AddError(description, node, true, quickFix);
        }

        void AddError(string desc, INodeModel node, bool isWarning, CompilerQuickFix quickFix)
        {
            errors.Add(new CompilerError { description = desc, sourceNode = node, sourceNodeGuid = node.Guid, isWarning = isWarning, quickFix = quickFix });
        }
    }

    [PublicAPI]
    public enum AssemblyType
    {
        None,
        Source,
        Memory,
        IlFile
    };
}
