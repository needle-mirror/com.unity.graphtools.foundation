using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using UnityEditor.EditorCommon.Utility;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using UnityEngine.VisualScripting;
using CompilationOptions = UnityEngine.VisualScripting.CompilationOptions;
using Debug = UnityEngine.Debug;

namespace UnityEditor.VisualScripting.Model.Translators
{
    public class RoslynTranslator : ITranslator
    {
        static readonly IEnumerable<string> k_DefaultNamespaces = new[]
        {
            "System",
            "System.Collections.Generic",
            "UnityEngine",
        };

        static int s_LastSyntaxTreeHashCode;
        static CompilationResult s_LastCompilationResult;

        int m_BuiltStackCounter;
        protected virtual HashSet<string> UsingDirectives { get; } = new HashSet<string>
        {
            "System",
            "System.Collections",
            "System.Collections.Generic",
            "System.Dynamic",
            "System.Linq",
            "Microsoft.CSharp",
            "UnityEngine",
            "UnityEngine.SceneManagement",
            "UnityEngine.VisualScripting"
        };

        protected virtual Dictionary<string, string> UsingAliases { get; } = new Dictionary<string, string>
        {
            { "UnityEngine.Object", "Object" },
            { "UnityEngine.Random", "Random" },
            { "UnityEngine.Debug", "Debug" },
            { "UnityEngine.SceneManagement.SceneManager", "SceneManager" }
        };

        public readonly Stack<MacroRefNodeModel> InMacro = new Stack<MacroRefNodeModel>();
        public HashSet<IStackModel> BuiltStacks = new HashSet<IStackModel>();

        public StackBaseModel EndStack { get; set; }
        public static bool LogCompileTimeStats { get; set; }
        public Stencil Stencil { get; }

        static RoslynTranslator()
        {
            LogCompileTimeStats = false;
        }

        public RoslynTranslator(Stencil stencil)
        {
            Stencil = stencil;
        }

        public void AddUsingDirectives(params string[] namespaces)
        {
            UsingDirectives.AddRange(namespaces.Where(n => !string.IsNullOrEmpty(n)));
        }

        public void AddUsingAlias(string alias, string aliasNamespace)
        {
            if (UsingAliases.ContainsKey(aliasNamespace))
                return;

            UsingAliases.Add(aliasNamespace, alias);
        }

        public bool SupportsCompilation() => true;

        public static Type KeySelector(MethodInfo mi)
        {
            return mi.GetParameters()[1].ParameterType;
        }

        public virtual void OnValidate(VSGraphModel graphModel, AssemblyType assemblyType, CompilationOptions compilationOptions, ref CompilationResult results)
        {
        }

        public CompilationResult TranslateAndCompile(VSGraphModel graphModel, AssemblyType assemblyType, CompilationOptions compilationOptions)
        {
            CompilationResult compilationResult = new CompilationResult();

            return compilationResult;
        }

        readonly List<CompilerError> m_Errors = new List<CompilerError>();

        public void AddError(INodeModel model, string error, CompilerQuickFix quickFix = null)
        {
            m_Errors.Add(new CompilerError { sourceNode = model, sourceNodeGuid = model.Guid, description = error, quickFix = quickFix, isWarning = false });
        }

        public void AddWarning(INodeModel model, string warning, CompilerQuickFix quickFix = null)
        {
            m_Errors.Add(new CompilerError { sourceNode = model, sourceNodeGuid = model.Guid, description = warning, quickFix = quickFix, isWarning = true });
        }

#if UNITY_EDITOR_WIN
        static string s_OSPathToData = Path.GetDirectoryName(EditorApplication.applicationPath) + "/Data";
#else
        static string s_OSPathToData = EditorApplication.applicationPath + "/Contents";
#endif
        static string s_RuntimePath = s_OSPathToData + "/MonoBleedingEdge/lib/mono/4.5/{0}.dll";

        static readonly IEnumerable<string> k_DefaultReferencesPaths =
            new[]
        {
            string.Format(s_RuntimePath, "mscorlib"),
            string.Format(s_RuntimePath, "System"),
        };

        public CompilationOptions Options;

        public enum PortSemantic { Read, Write }

        // This will find a common descendant stack for both inputs
        // ie. FindCommonDescendant(A, B) => E
        // Note: if c wasn't connected to E, A and B would not have a common descendant,
        // as no descendant of B would be reachable from C
        //        Root
        //       /   \
        //      A     B
        //     / \    |
        //     C  D   |
        //      \__\ /
        //          E
        // we need to keep two sets/queues, one per initial branch
        // another solution would be to keep an Ancestor hashset associated to each stack model
        // and find a non empty union
        public static StackBaseModel FindCommonDescendant(IStackModel root, StackBaseModel a, StackBaseModel b)
        {
            var stackModels = new HashSet<IStackModel>();
            if (root != a && root != b)
                stackModels.Add(root);
            return FindCommonDescendant(stackModels, a, b);
        }

        public static StackBaseModel FindCommonDescendant(HashSet<IStackModel> visited, StackBaseModel a, StackBaseModel b)
        {
            if (a == b) // FCD(a, a) = a
                return a;

            if (a == null || b == null) // FCD(x, null) = null
                return null;

            HashSet<StackBaseModel> aSet = new HashSet<StackBaseModel>();
            aSet.Add(a);
            Queue<StackBaseModel> aQueue = new Queue<StackBaseModel>();
            aQueue.Enqueue(a);

            HashSet<StackBaseModel> bSet = new HashSet<StackBaseModel>();
            bSet.Add(b);
            Queue<StackBaseModel> bQueue = new Queue<StackBaseModel>();
            bQueue.Enqueue(b);

            while (aQueue.Count > 0 || bQueue.Count > 0)
            {
                if (aQueue.Count > 0)
                    a = aQueue.Dequeue();
                if (bQueue.Count > 0)
                    b = bQueue.Dequeue();
                if (Test(ref a, aSet, bSet, aQueue, visited))
                    return a;
                if (Test(ref b, bSet, aSet, bQueue, visited))
                    return b;
            }

            return null;
        }

        // check if thisStack's descendants have been already visited and put in otherBranchSet
        // otherwise add them
        static bool Test(ref StackBaseModel thisStack,
            HashSet<StackBaseModel> thisBranchSet,
            HashSet<StackBaseModel> otherBranchSet,
            Queue<StackBaseModel> thisBranchQueue, HashSet<IStackModel> visited)
        {
            if (thisStack == null)
                return false;

            StackBaseModel connectedStack = FindConnectedStacksCommonDescendant(thisStack, visited);
            if (!connectedStack)
                return false;

            Assert.IsTrue(connectedStack.InputPorts.Count > 0, "a connected stack must have inputs");

            // small optimization: no common descendant can have less than 2 input connections
            if (connectedStack.InputPorts.Sum(c => c.ConnectionPortModels.Count()) >= 2 &&
                otherBranchSet.Contains(connectedStack))
            {
                thisStack = connectedStack;
                return true;
            }

            if (thisBranchSet.Add(connectedStack))
                thisBranchQueue.Enqueue(connectedStack);

            return false;
        }

        static StackBaseModel FindConnectedStacksCommonDescendant(INodeModel statement, HashSet<IStackModel> visited)
        {
            var firstStack = GetConnectedStack((NodeModel)statement, 0);
            StackBaseModel desc = statement.OutputsByDisplayOrder.Aggregate(firstStack, (stack, nextPort) =>
            {
                if (stack == null)
                    return null;
                if (nextPort.PortType != PortType.Execution)
                    return firstStack;
                var nextStack = GetConnectedStack(nextPort);
                if (nextStack == null)
                    return null;
                if (!visited.Add(nextStack))
                    return stack;
                return FindCommonDescendant(visited, stack, nextStack);
            });
            return desc;
        }

        public static IEnumerable<StackBaseModel> GetConnectedStacks(INodeModel statement)
        {
            return statement.OutputsByDisplayOrder.Select(x => x.ConnectionPortModels.FirstOrDefault()?.NodeModel as StackBaseModel);
        }

        public static StackBaseModel GetConnectedStack(IPortModel port)
        {
            return port.ConnectionPortModels.FirstOrDefault()?.NodeModel as StackBaseModel;
        }

        // TODO investigate if usages are valid
        public static StackBaseModel GetConnectedStack(NodeModel statement, int index)
        {
            return statement.OutputsByDisplayOrder.ElementAt(index).ConnectionPortModels.FirstOrDefault()?.NodeModel as StackBaseModel;
        }

        Dictionary<string, int> m_UniqueNames = new Dictionary<string, int>();

        public string MakeUniqueName(string name)
        {
            if (!m_UniqueNames.TryGetValue(name, out var count))
            {
                m_UniqueNames.Add(name, 1);
                return name;
            }

            m_UniqueNames[name] = count + 1;
            return name + count;
        }

        public void ClearBuiltStacks()
        {
            BuiltStacks.Clear();
        }

        public void RegisterBuiltStack(IStackModel stackModel)
        {
            if (!BuiltStacks.Add(stackModel))
                throw new LoopDetectedException($"stack already built: {stackModel.GetId()} number of inputs: {stackModel.InputPorts[0].ConnectionPortModels.Count()}");
        }
    }

    public enum StackExitStrategy
    {
        Inherit,
        Return,
        Continue,
        Break,
        None
    }
}
