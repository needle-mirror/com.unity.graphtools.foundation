using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class EditorTitle
{
    static string s_RepoPath;

    static EditorTitle()
    {
        if (Unsupported.IsDeveloperMode())
        {
            EditorApplication.updateMainWindowTitle += EditorApplicationOnUpdateMainWindowTitle;
            EditorApplication.UpdateMainWindowTitle();
        }
    }

    static void EditorApplicationOnUpdateMainWindowTitle(ApplicationTitleDescriptor obj)
    {
        if (s_RepoPath == null)
        {
            try
            {
                // DataPath: C:\path\to\repo\VS\Assets
                // => repo
                s_RepoPath = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(Application.dataPath)));
            }
            catch
            {
                s_RepoPath = "???";
            }
        }

        obj.title = $"{s_RepoPath}/{obj.title}";
    }
}
