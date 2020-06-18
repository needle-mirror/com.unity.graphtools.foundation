using System;
using System.IO;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public static class ModelUtility
    {
        static readonly string k_CompileScriptsOutputDirectory = Path.Combine(Environment.CurrentDirectory, "Library", "VisualScripting");
        static readonly string k_AssemblyRelativePath = Path.Combine("Assets", "Runtime", "VisualScripting");
        static readonly string k_AssemblyOutputDirectory = Path.Combine(Environment.CurrentDirectory, k_AssemblyRelativePath);

        public static string GetCompileScriptsOutputDirectory()
        {
            return k_CompileScriptsOutputDirectory;
        }

        public static string GetAssemblyOutputDirectory()
        {
            return k_AssemblyOutputDirectory;
        }

        public static string GetAssemblyRelativePath()
        {
            return k_AssemblyRelativePath;
        }
    }
}
