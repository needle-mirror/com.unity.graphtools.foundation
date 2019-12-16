using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using UnityEditor.EditorCommon.Utility;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEditor.VisualScripting.Plugins;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
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
        StackExitStrategy m_StackExitStrategy = StackExitStrategy.Return;
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
        public static LanguageVersion LanguageVersion => LanguageVersion.Latest;

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

        public static bool FilterMethods(MethodInfo mi)
        {
            return mi.ReturnType == typeof(IEnumerable<SyntaxNode>) &&
                mi.GetParameters().Length == 3 &&
                mi.GetParameters()[2].ParameterType == typeof(IPortModel);
        }

        public static Type KeySelector(MethodInfo mi)
        {
            return mi.GetParameters()[1].ParameterType;
        }

        public virtual void OnValidate(VSGraphModel graphModel, AssemblyType assemblyType, CompilationOptions compilationOptions, ref CompilationResult results)
        {
        }

        public virtual Microsoft.CodeAnalysis.SyntaxTree OnTranslate(VSGraphModel graphModel, AssemblyType assemblyType, CompilationOptions compilationOptions, ref CompilationResult compilationResult)
        {
            const string windowsLineEndings = "\r\n";
            const string unixLineEndings = "\n";

            Microsoft.CodeAnalysis.SyntaxTree syntaxTree = Translate(graphModel, compilationOptions); // we will measure plugins time later

            string preferredLineEndings;
            LineEndingsMode lineEndingsForNewScripts = EditorSettings.lineEndingsForNewScripts;

            switch (lineEndingsForNewScripts)
            {
                case LineEndingsMode.OSNative:
                    preferredLineEndings = Application.platform == RuntimePlatform.WindowsEditor ? windowsLineEndings : unixLineEndings;
                    break;
                case LineEndingsMode.Unix:
                    preferredLineEndings = unixLineEndings;
                    break;
                case LineEndingsMode.Windows:
                    preferredLineEndings = windowsLineEndings;
                    break;
                default:
                    preferredLineEndings = unixLineEndings;
                    break;
            }

            var adHocWorkspace = new AdhocWorkspace();

            var options = adHocWorkspace.Options
                .WithChangedOption(CSharpFormattingOptions.NewLineForMembersInObjectInit, true)
                .WithChangedOption(CSharpFormattingOptions.WrappingPreserveSingleLine, false)
                .WithChangedOption(CSharpFormattingOptions.WrappingKeepStatementsOnSingleLine, false)
                .WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers, true)
                .WithChangedOption(FormattingOptions.NewLine, LanguageNames.CSharp, preferredLineEndings);

            compilationResult.sourceCode[(int)SourceCodePhases.Initial] = syntaxTree.GetText().ToString();

            var formattedTree = Formatter.Format(syntaxTree.GetCompilationUnitRoot(), adHocWorkspace, options);
            formattedTree = new VisualScriptingCSharpFormatter().Visit(formattedTree);
            string codeText = formattedTree.GetText().ToString();

            compilationResult.sourceCode[(int)SourceCodePhases.Final] = codeText;

            return syntaxTree;
        }

        public CompilationResult TranslateAndCompile(VSGraphModel graphModel, AssemblyType assemblyType, CompilationOptions compilationOptions)
        {
            string graphModelSourceFilePath = graphModel.SourceFilePath;

            CompilationResult compilationResult = new CompilationResult();
            compilationResult.sourceCode = new string[Enum.GetNames(typeof(SourceCodePhases)).Length];

            long translationTime = 0;
            long analysisTime = 0;
            long compilationTime = 0;

            Stopwatch sw = Stopwatch.StartNew();

            Profiler.BeginSample("Validation");
            OnValidate(graphModel, assemblyType, compilationOptions, ref compilationResult);
            long validationTime = sw.ElapsedMilliseconds;
            Profiler.EndSample();

            compilationResult.errors.AddRange(m_Errors);
            m_Errors.Clear();

            if (compilationResult.status == CompilationStatus.Succeeded)
            {
                sw.Restart();
                Profiler.BeginSample("Translation");

                var syntaxTree = OnTranslate(graphModel, assemblyType, compilationOptions, ref compilationResult);

                compilationResult.errors.AddRange(m_Errors);
                m_Errors.Clear();

                translationTime = sw.ElapsedMilliseconds;
                Profiler.EndSample();

                sw.Restart();
                Profiler.BeginSample("Compilation");

                if (compilationResult.status == CompilationStatus.Succeeded)
                {
                    int syntaxTreeHashCode = syntaxTree.ToString().GetHashCode();
                    if (s_LastSyntaxTreeHashCode == syntaxTreeHashCode)
                    {
                        compilationResult = s_LastCompilationResult;
                        if (LogCompileTimeStats)
                            Debug.Log("Reused cached compilation result");
                    }
                    else
                    {
                        try
                        {
                            if (LogCompileTimeStats)
                                Debug.Log("Compute new compilation result");
                            compilationResult = CheckSemanticModel(syntaxTree, compilationResult);
                        }
                        catch (LoopDetectedException e)
                        {
                            compilationResult.AddError(e.Message);
                        }
                        finally
                        {
                            s_LastSyntaxTreeHashCode = syntaxTreeHashCode;
                            s_LastCompilationResult = compilationResult;
                        }
                    }

                    Profiler.BeginSample("WriteSource");
                    if (assemblyType == AssemblyType.Source)
                    {
                        string directoryName = Path.GetDirectoryName(graphModelSourceFilePath);
                        Assert.IsNotNull(directoryName, nameof(directoryName) + " != null");
                        Directory.CreateDirectory(directoryName);

                        string sourceCode = compilationResult.sourceCode[(int)SourceCodePhases.Final];

                        if (compilationResult.status == CompilationStatus.Succeeded)
                        {
                            File.WriteAllText(graphModelSourceFilePath, sourceCode);
                        }
                    }
                    Profiler.EndSample();
                }
                compilationTime = sw.ElapsedMilliseconds;
                Profiler.EndSample();
            }

            // needs to be done on the main thread
            foreach (CompilerError error in compilationResult.errors)
                if (error.sourceNodeGuid == default && error.sourceNode != null && !error.sourceNode.Destroyed)
                    error.sourceNodeGuid = error.sourceNode.Guid;
                else
                    graphModel.NodesByGuid.TryGetValue(error.sourceNodeGuid, out error.sourceNode);

            if (LogCompileTimeStats)
            {
                Debug.Log($"Validation: {validationTime}ms Translation: {translationTime}ms Code Analysis: {analysisTime}ms Compilation: {compilationTime}ms");
            }

            return compilationResult;
        }

        readonly List<CompilerError> m_Errors = new List<CompilerError>();

        public void AddError(INodeModel model, string error, CompilerQuickFix quickFix = null)
        {
            m_Errors.Add(new CompilerError {sourceNode = model, sourceNodeGuid = model.Guid, description = error, quickFix = quickFix,  isWarning = false});
        }

        public void AddWarning(INodeModel model, string warning, CompilerQuickFix quickFix = null)
        {
            m_Errors.Add(new CompilerError {sourceNode = model, sourceNodeGuid = model.Guid, description = warning, quickFix = quickFix,  isWarning = true});
        }

        public Microsoft.CodeAnalysis.SyntaxTree Translate(VSGraphModel graphModel, CompilationOptions options)
        {
            Profiler.BeginSample("Translation");
            Microsoft.CodeAnalysis.SyntaxTree syntaxTree = ToSyntaxTree(graphModel, options);
            Profiler.EndSample();
            if (syntaxTree is CSharpSyntaxTree cSharpSyntaxTree && syntaxTree.TryGetRoot(out var treeRoot))
            {
                var treeOptions = cSharpSyntaxTree.Options.WithLanguageVersion(LanguageVersion);
                return syntaxTree.WithRootAndOptions(treeRoot, treeOptions);
            }
            return syntaxTree;
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

        static readonly IEnumerable<MetadataReference> k_DefaultReferences =
            (
                from p in k_DefaultReferencesPaths
                where (File.Exists(p))
                select MetadataReference.CreateFromFile(p)
            ).ToArray();

        static List<MetadataReference> s_CachedAllReferences = GetAllReferences();

        static List<MetadataReference> GetAllReferences()
        {
            List<MetadataReference> cachedAllReferences = new List<MetadataReference>();

            cachedAllReferences.AddRange(k_DefaultReferences);
            Assembly[] assemblies = (from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                where !domainAssembly.IsDynamic &&
                domainAssembly.Location != ""
                select domainAssembly).ToArray();
            foreach (var assembly in assemblies)
                cachedAllReferences.Add(MetadataReference.CreateFromFile(assembly.Location));

            return cachedAllReferences;
        }

        static SemanticModel GetSemanticModel(Microsoft.CodeAnalysis.SyntaxTree syntaxTree)
        {
            Profiler.BeginSample("CreateCompilation");
            CSharpCompilation compilation = CSharpCompilation.Create("VSScriptCompilation", options: CompilationOptions,
                syntaxTrees: new[] { syntaxTree }, references: s_CachedAllReferences);
            Profiler.EndSample();
            Profiler.BeginSample("GetSemModel");
            SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);
            Profiler.EndSample();
            return semanticModel;
        }

        static CSharpCompilationOptions s_CachedCompilationOptions;

        static CSharpCompilationOptions CompilationOptions
        {
            get
            {
                if (s_CachedCompilationOptions == null)
                {
                    s_CachedCompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                        .WithOverflowChecks(true)
                        .WithOptimizationLevel(OptimizationLevel.Debug)
                        .WithReportSuppressedDiagnostics(false)
                        .WithWarningLevel(0)
                        .WithUsings(k_DefaultNamespaces);
                }

                return s_CachedCompilationOptions;
            }
        }

        static CompilationResult CheckSemanticModel(Microsoft.CodeAnalysis.SyntaxTree syntaxTree,
            CompilationResult compilationResult)
        {
            SemanticModel semanticModel = GetSemanticModel(syntaxTree);

            // run all the semantic code analyzers
            var diagnostics = new List<CompilerError>();
            Profiler.BeginSample("GetDiagnostics");
            ImmutableArray<Diagnostic> rawDiagnostics = semanticModel.GetDiagnostics();
            Profiler.EndSample();

            Profiler.BeginSample("ProcessDiagnostics");
            ProcessDiagnostics(rawDiagnostics, diagnostics);
            Profiler.EndSample();

            if (diagnostics.Any())
            {
                compilationResult.errors = diagnostics;
            }

            return compilationResult;
        }

        static void ProcessDiagnostics(ImmutableArray<Diagnostic> rawDiagnostics, List<CompilerError> diagnostics)
        {
            foreach (var diagnostic in rawDiagnostics)
            {
                if (diagnostic.Severity != DiagnosticSeverity.Error)
                    continue;

                var syntaxNode = diagnostic.Location.SourceTree.GetRoot().FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                SyntaxNode firstAncestorWithAnnotation = null;
                if (syntaxNode.HasAnnotations(Annotations.VSNodeMetadata))
                {
                    firstAncestorWithAnnotation = syntaxNode;
                }
                else
                {
                    foreach (var ancestor in syntaxNode.Ancestors())
                    {
                        if (ancestor.HasAnnotations(Annotations.VSNodeMetadata))
                        {
                            firstAncestorWithAnnotation = ancestor;
                            break;
                        }

                        // TODO Fix for drop-4. Need Spike on this (UX + Programming)
                        if (ancestor.HasAnnotations(Annotations.VariableAnnotationKind))
                        {
                            Debug.LogError(diagnostic);
                            break;
                        }
                    }
                }

                SyntaxAnnotation smAnnotation = null;
                if (firstAncestorWithAnnotation != null)
                    smAnnotation = firstAncestorWithAnnotation.GetAnnotations(Annotations.VSNodeMetadata).FirstOrDefault();

                var compilerError = new CompilerError { description = diagnostic.ToString() };

                if (smAnnotation != null)
                    GUID.TryParse(smAnnotation.Data, out compilerError.sourceNodeGuid);

                diagnostics.Add(compilerError);
            }
        }

        public void AddMember(MemberDeclarationSyntax x)
        {
            m_AllFields.Add(x);
        }

        public CompilationOptions Options;
        List<StatementSyntax> m_EventRegistrations = new List<StatementSyntax>();
        protected virtual Microsoft.CodeAnalysis.SyntaxTree ToSyntaxTree(VSGraphModel graphModel, CompilationOptions options)
        {
            //TODO fix graph name, do not use the asset name
            var className = graphModel.TypeName;
            var baseClass = graphModel.Stencil.GetBaseClass().Name;
            var classDeclaration = ClassDeclaration(className)
                .WithModifiers(
                TokenList(
                    Token(SyntaxKind.PublicKeyword)));

            if (!String.IsNullOrEmpty(baseClass))
            {
                classDeclaration = classDeclaration.WithBaseList(
                    BaseList(
                        SingletonSeparatedList<BaseTypeSyntax>(
                            SimpleBaseType(
                                IdentifierName(baseClass)))));
            }

            if (graphModel.Stencil.addCreateAssetMenuAttribute)
            {
                classDeclaration = classDeclaration.WithAttributeLists(
                    SingletonList(
                        AttributeList(
                            SingletonSeparatedList(
                                Attribute(
                                    IdentifierName("CreateAssetMenu"))
                                    .WithArgumentList(
                                    AttributeArgumentList(
                                        SeparatedList<AttributeArgumentSyntax>(
                                            new SyntaxNodeOrToken[]
                                            {
                                                AttributeArgument(
                                                    LiteralExpression(
                                                        SyntaxKind.StringLiteralExpression,
                                                        Literal(graphModel.Stencil.fileName)))
                                                    .WithNameEquals(
                                                    NameEquals(
                                                        IdentifierName("fileName"))),
                                                Token(SyntaxKind.CommaToken),
                                                AttributeArgument(
                                                    LiteralExpression(
                                                        SyntaxKind.StringLiteralExpression,
                                                        Literal(graphModel.Stencil.menuName)))
                                                    .WithNameEquals(
                                                    NameEquals(
                                                        IdentifierName("menuName")))
                                            })))))));
            }

            var allMembers = new List<MemberDeclarationSyntax>();
            m_AllFields = new List<MemberDeclarationSyntax>();
            var allRemainingMembers = new List<MemberDeclarationSyntax>();

            foreach (var fieldDecl in graphModel.GraphVariableModels)
            {
                var fieldSyntaxNode = fieldDecl.DeclareField(this);
                m_AllFields.Add(fieldSyntaxNode);
            }

            var entryPoints = graphModel.GetEntryPoints();

            Dictionary<string, MethodDeclarationSyntax> declaredMethods = new Dictionary<string, MethodDeclarationSyntax>();
            foreach (var stack in entryPoints)
            {
                var entrySyntaxNode = BuildNode(stack);
                foreach (var memberDeclaration in entrySyntaxNode.Cast<MemberDeclarationSyntax>())
                {
                    if (memberDeclaration is MethodDeclarationSyntax methodDeclarationSyntax)
                    {
                        string key = methodDeclarationSyntax.Identifier.ToString();
                        declaredMethods.Add(key, methodDeclarationSyntax);
                    }
                    else
                        allRemainingMembers.Add(memberDeclaration);
                }
            }

            allMembers.AddRange(m_AllFields);
            m_AllFields = null;
            allMembers.AddRange(allRemainingMembers);

            if (m_EventRegistrations.Any())
            {
                if (!declaredMethods.TryGetValue("Update", out var method))
                {
                    method = RoslynBuilder.DeclareMethod("Update", AccessibilityFlags.Public, typeof(void))
                        .WithParameterList(
                            ParameterList(
                                SeparatedList(
                                    Enumerable.Empty<ParameterSyntax>())))
                        .WithBody(Block());
                }

                BlockSyntax blockSyntax = Block(m_EventRegistrations.Concat(method.Body.Statements));

                method = method.WithBody(blockSyntax);
                declaredMethods["Update"] = method;
            }

            allMembers.AddRange(declaredMethods.Values);
            classDeclaration = classDeclaration.AddMembers(allMembers.ToArray());

            var referencedNamespaces = UsingDirectives.OrderBy(n => n).Select(ns => UsingDirective(ParseName(ns)));
            var namespaceAliases = UsingAliases.OrderBy(n => n.Key).Select(pair =>
                UsingDirective(ParseName(pair.Key))
                    .WithAlias(NameEquals(
                    IdentifierName(pair.Value))));
            var usings = referencedNamespaces.Concat(namespaceAliases).ToArray();
            var compilationUnit = CompilationUnit()
                .WithUsings(
                List(usings))
                .WithMembers(
                    SingletonList<MemberDeclarationSyntax>(classDeclaration)).NormalizeWhitespace();

            return compilationUnit.SyntaxTree;
        }

        public virtual void BuildStack(IStackModel stack, ref BlockSyntax blockNode,
            StackExitStrategy exitStrategy = StackExitStrategy.Return)
        {
            if (stack == null || stack.State == ModelState.Disabled)
                return;

            RegisterBuiltStack(stack);

            // JUST in case... until we validate the previous failsafe
            if (m_BuiltStackCounter++ > 10000)
                throw new InvalidOperationException("Infinite loop while building the script, aborting");

            StackExitStrategy origStackExitStrategy = m_StackExitStrategy;
            if (exitStrategy != StackExitStrategy.Inherit)
                m_StackExitStrategy = exitStrategy;

//            Debug.Log($"Build stack {stack}");
            // m_EndStack might get changed by recursive BuildNode() calls
            StackBaseModel origEndStack = EndStack;
            var statements = new List<StatementSyntax>();
            foreach (var statement in stack.NodeModels)
            {
                if (statement.State == ModelState.Disabled)
                    continue;
                var syntaxNodes = BuildNode(statement);
                foreach (var syntaxNode in syntaxNodes)
                {
                    StatementSyntax resultingStatement;
                    switch (syntaxNode)
                    {
                        case StatementSyntax statementNode:
                            resultingStatement = statementNode;
                            break;
                        case ExpressionSyntax expressionNode:
                            resultingStatement = ExpressionStatement(expressionNode);
                            break;
                        default:
                            throw new InvalidOperationException($"Expected a statement or expression node, found a {syntaxNode.GetType()} when building {statement}");
                    }

                    // TODO: RecordValue codegen counter instead of counting them after the fact
                    if ((Options & UnityEngine.VisualScripting.CompilationOptions.Tracing) != 0)
                    {
                        var recordValueCount = syntaxNode.GetAnnotations(Annotations.RecordValueCountKind).FirstOrDefault();
                        int recordedValuesCount = recordValueCount == null
                            ? syntaxNode.GetAnnotatedNodes(Annotations.RecordValueKind).Count()
                            : int.Parse(recordValueCount.Data);
                        statements.Add(InstrumentForInEditorDebugging.BuildLastCallFrameExpression(recordedValuesCount, statement.Guid, this.GetRecorderName()));
                    }
                    statements.Add(resultingStatement);
                }
            }

            blockNode = blockNode.AddStatements(statements.ToArray());

            if (stack.DelegatesOutputsToNode(out _))
            {
                var nextStack = EndStack;
                m_StackExitStrategy = origStackExitStrategy;

//                Debug.Log($"Stack {stack} delegates ports. nextStack {nextStack} origEndStack {origEndStack}");

                // if a nested node changed the end stack, but found the same common descendant,
                // let the parent call handle it
                if (EndStack == origEndStack)
                    return;

                EndStack = origEndStack;
                BuildStack(nextStack, ref blockNode, exitStrategy);

                return;
            }

            bool anyConnection = false;

            foreach (var outputPort in stack.OutputPorts)
            {
                foreach (var connectedStack in outputPort.ConnectionPortModels)
                {
                    if (connectedStack.NodeModel is IStackModel nextStack)
                    {
                        anyConnection = true;
                        if (!ReferenceEquals(nextStack, EndStack))
                            BuildStack(nextStack, ref blockNode, StackExitStrategy.Inherit);
                    }
                }
            }


            // TODO use function default return value
            StatementSyntax lastStatementSyntax = blockNode.Statements.LastOrDefault();
            if (!anyConnection && !(lastStatementSyntax is ReturnStatementSyntax) && !(lastStatementSyntax is ContinueStatementSyntax))
            {
                //TODO we had that for a reason
//                lastStatementSyntax = StatementFromExitStrategy(m_StackExitStrategy, null);

//                if(lastStatementSyntax != null)
//                    blockNode = blockNode.AddStatements(lastStatementSyntax);
            }

            m_StackExitStrategy = origStackExitStrategy;
        }

        protected virtual IdentifierNameSyntax GetRecorderName() => IdentifierName("recorder");

//        static StatementSyntax StatementFromExitStrategy(StackExitStrategy stackExitStrategy, ExpressionSyntax returnValue)
//        {
//            StatementSyntax lastStatementSyntax;
//            switch (stackExitStrategy)
//            {
//                case StackExitStrategy.Return:
//                    lastStatementSyntax = ReturnStatement(returnValue);
//                    break;
//                case StackExitStrategy.Break:
//                    lastStatementSyntax = BreakStatement();
//                    break;
//                case StackExitStrategy.None:
//                    lastStatementSyntax = null;
//                    break;
//                default:
//                    lastStatementSyntax = ContinueStatement();
//                    break;
//            }
//
//            return lastStatementSyntax;
//        }

        protected virtual IEnumerable<SyntaxNode> BuildNode(INodeModel statement, IPortModel portModel)
        {
            if (statement == null)
                return Enumerable.Empty<SyntaxNode>();
            Assert.IsTrue(portModel == null || portModel.NodeModel == statement, "If a Port is provided, it must be owned by the provided node");
            var ext = ModelUtility.ExtensionMethodCache<RoslynTranslator>.GetExtensionMethod(
                statement.GetType(),
                FilterMethods,
                KeySelector);
            if (ext != null)
            {
                var syntaxNode = (IEnumerable<SyntaxNode>)ext.Invoke(null, new object[] { this, statement, portModel }) ?? Enumerable.Empty<SyntaxNode>();
                var annotatedNodes = new List<SyntaxNode>();
                foreach (var node in syntaxNode)
                {
                    var annotatedNode = node?.WithAdditionalAnnotations(new SyntaxAnnotation(Annotations.VSNodeMetadata, statement.Guid.ToString()));
                    annotatedNodes.Add(annotatedNode);
                }

                return annotatedNodes;
            }
            else
            {
                Debug.LogError("Roslyn Translator doesn't know how to create a node of type: " + statement.GetType());
            }

            return Enumerable.Empty<SyntaxNode>();
        }

        public enum PortSemantic { Read, Write }
        public IEnumerable<SyntaxNode> BuildPort(IPortModel portModel, PortSemantic portSemantic = PortSemantic.Read)
        {
            var buildPortInner = BuildPortInner(portModel, out var builtNode).ToList();
            if (portSemantic == PortSemantic.Read && (Options & UnityEngine.VisualScripting.CompilationOptions.Tracing) != 0 &&
                builtNode != null && buildPortInner.Count == 1 && buildPortInner.First() is ExpressionSyntax exp)
                return Enumerable.Repeat(InstrumentForInEditorDebugging.RecordValue(GetRecorderName(), exp, null, (NodeModel)builtNode), 1);
            return buildPortInner;
        }

        IEnumerable<SyntaxNode> BuildPortInner(IPortModel portModel, out INodeModel builtNode)
        {
            var connectedPort = portModel.ConnectionPortModels.FirstOrDefault();
            if (connectedPort != null && connectedPort.NodeModel.State == ModelState.Enabled)
            {
                return BuildNode(builtNode = connectedPort.NodeModel, connectedPort);
            }

            // if it' s an embedded value, no point in recording its value, keep a null builtNode
            builtNode = null;
            if (portModel.EmbeddedValue != null)
                return BuildNode(portModel.EmbeddedValue);

            // return default datatype value (null) if not connected. not recorded either
            var defaultValue = RoslynBuilder.GetDefault(portModel.DataType.Resolve(Stencil));
            return Enumerable.Repeat(defaultValue == null
                ? LiteralExpression(SyntaxKind.NullLiteralExpression)
                : Constant(defaultValue, Stencil), 1);
        }

        public IEnumerable<SyntaxNode> BuildNode(INodeModel inputNode)
        {
            return inputNode == null ? Enumerable.Empty<SyntaxNode>() : BuildNode(inputNode, null);
        }

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

        public void AddEventRegistration(StatementSyntax registerEventHandlerCall)
        {
            m_EventRegistrations.Add(registerEventHandlerCall);
        }

        Dictionary<string, int> m_UniqueNames = new Dictionary<string, int>();
        List<MemberDeclarationSyntax> m_AllFields;

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

        public virtual ExpressionSyntax Constant(object value, Stencil stencil, Type generatedType = null)
        {
            switch (value)
            {
                case float f:
                    return LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(f));
                case decimal d:
                    return LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(d));
                case int i:
                    return LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(i));
                case double d:
                    return LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(d));
                case bool b:
                    return LiteralExpression(b ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);
                case string s:
                    return LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(s));
                case char c:
                    return LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(c));
                case Vector2 _:
                case Vector3 _:
                case Vector4 _:
                case Quaternion _:
                case Color _:

                    if (generatedType == null)
                        generatedType = value.GetType();
                    return RoslynBuilder.CreateConstantInitializationExpression(value, generatedType);
                case Enum _:
                    return MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(value.GetType().Name),
                        IdentifierName(value.ToString())
                    );
                case EnumValueReference e:
                    return MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        TypeSystem.BuildTypeSyntax(e.EnumType.Resolve(stencil)),
                        IdentifierName(e.ValueAsEnum(stencil).ToString())
                    );
                case AnimationCurve _:
                    return DefaultExpression(TypeSyntaxFactory.ToTypeSyntax(typeof(AnimationCurve)));
                case LayerMask m:
                    return LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(m.value));
                case InputName inputName:
                    return inputName.name == null
                        ? LiteralExpression(SyntaxKind.NullLiteralExpression)
                        : LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(inputName.name));
                case SceneAsset asset:
                    return LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(AssetDatabase.GetAssetPath(asset)));
                default:
                    return DefaultExpression(TypeSystem.BuildTypeSyntax(value.GetType()));
            }
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
