using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Utils;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class UnityCompilerTestHelper
{
    static Program StartCsc(string arguments)
    {
        var csc = Paths.Combine(EditorApplication.applicationContentsPath, "Tools", "RoslynScripts", "unity_csc");
        if (Application.platform == RuntimePlatform.WindowsEditor)
        {
            csc += ".bat";
        }
        else
        {
            csc += ".sh";
        }

        csc = Paths.UnifyDirectorySeparator(csc);

        if (!File.Exists(csc))
        {
            Debug.LogError($"file not found {csc}");
            return null;
        }

        var psi = new ProcessStartInfo() { Arguments = arguments, FileName = csc, CreateNoWindow = true };
        var program = new Program(psi);
        program.Start();
        return program;
    }

    public static string GetCompilerVersion()
    {
        var program = StartCsc("-langversion:?"); // spits versions numbers one per line
        program.WaitForExit(20 * 1000); // give plenty of time to test machines
        // look for line like "7.3 (latest)"
        var line = program.GetStandardOutput().FirstOrDefault(s => s.Contains("latest"));
        return line?.Split(' ').First();
    }
}
