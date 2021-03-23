using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Compilation;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.VisualScripting;

namespace UnityEditor.VisualScripting.Model.Compilation
{
    class RoslynCompiler : IBuilder
    {
        public event Action<string, CompilerMessage[]> BuildFinished;
        AssemblyBuilder m_AssemblyBuilder;

        static readonly IEnumerable<string> k_DefaultReferencesPaths =
            new[]
        {
            string.Format("Library/ScriptAssemblies/{0}.dll", "Assembly-CSharp"),
        };
        public static IBuilder DefaultBuilder = new RoslynCompiler();

        public void BuildVisualScriptingAssemblyFromAllSourceFiles(string outputDirectory)
        {
            // check if output folder exists
            Directory.CreateDirectory(ModelUtility.GetCompileScriptsOutputDirectory());
            Directory.CreateDirectory(outputDirectory);

            var scriptSourcePath = ModelUtility.GetCompileScriptsOutputDirectory();
            var sourceFiles = Directory.GetFiles(scriptSourcePath);

            if (sourceFiles.Length == 0)
            {
                OnBuildFinished(string.Empty, new CompilerMessage[] { });
                return;
            }

            // use build pipeline to generate assembly
            var assemblyOutputPath = Path.Combine(outputDirectory, "VisualScriptingAssembly-CSharp.dll");
            m_AssemblyBuilder = new AssemblyBuilder(assemblyOutputPath, sourceFiles);
            var defaultReferences = new HashSet<string>(m_AssemblyBuilder.defaultReferences);
            var assemblies = (from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                              where !domainAssembly.IsDynamic &&
                              domainAssembly.Location != "" &&
                              ((domainAssembly.Location.Contains("UnityEngine") && !domainAssembly.Location.Contains("Module.dll")) ||
                                  domainAssembly.Location.Contains("Unity.GraphTools.Foundation.dll")) &&
                              !domainAssembly.Location.Contains("Tests") &&
                              !defaultReferences.Contains(domainAssembly.Location)
                              select domainAssembly).ToArray();

            HashSet<string> additionalReferences = new HashSet<string>();

            foreach (var assembly in assemblies)
                additionalReferences.Add(assembly.Location);

            foreach (var defaultReference in k_DefaultReferencesPaths)
                if (File.Exists(defaultReference))
                    additionalReferences.Add(defaultReference);

            m_AssemblyBuilder.additionalReferences = additionalReferences.ToArray();

            m_AssemblyBuilder.buildFinished += OnBuildFinished;
            var result = m_AssemblyBuilder.Build();

            if (result == false)
            {
                Debug.LogError("CodeGenCompilation failed to build assembly.");
            }
        }

        void OnBuildFinished(string assemblyPath, CompilerMessage[] messages)
        {
            if (messages.Any())
            {
                foreach (var error in messages)
                {
                    if (error.type == CompilerMessageType.Error)
                        Debug.LogError(error.message);
                    if (error.type == CompilerMessageType.Warning)
                        Debug.LogWarning(error.message);
                }
            }

            // cleanup
            if (m_AssemblyBuilder != null)
            {
                m_AssemblyBuilder.buildFinished -= OnBuildFinished;
                m_AssemblyBuilder = null;
            }
            BuildFinished?.Invoke(assemblyPath, messages);

            // refresh all assets to trigger a domain reload
            AssetDatabase.Refresh();
        }

        public void Build(IEnumerable<GraphAssetModel> graphAssetModels, Action<string, CompilerMessage[]> roslynCompilationOnBuildFinished)
        {
            var scriptSourcePath = ModelUtility.GetCompileScriptsOutputDirectory();

            if (Directory.Exists(scriptSourcePath))
            {
                foreach (var path in Directory.GetFiles(scriptSourcePath))
                    File.Delete(path);
            }
            else
            {
                Directory.CreateDirectory(scriptSourcePath);
            }

            foreach (var graphAssetModel in graphAssetModels)
            {
                var vsGraphModel = (VSGraphModel)graphAssetModel.GraphModel;
                var translator = vsGraphModel.CreateTranslator();
                if (!translator.SupportsCompilation())
                    continue;

                vsGraphModel.Compile(AssemblyType.Source, translator,
                    CompilationOptions.Default | CompilationOptions.LiveEditing);
            }

            if (roslynCompilationOnBuildFinished != null)
                BuildFinished += roslynCompilationOnBuildFinished;

            BuildVisualScriptingAssemblyFromAllSourceFiles(ModelUtility.GetAssemblyOutputDirectory());
        }
    }
}
