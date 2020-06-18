using UnityEditor;
using UnityEngine;

public static class MathBookHelper
{
    const string kAssetDirectory = "Assets/Temp";
    const string kAssetPath = kAssetDirectory + "/TestModel.asset";

    public static string GetAssetPath()
    {
        System.IO.Directory.CreateDirectory(kAssetDirectory);
        return kAssetPath;
    }

    [MenuItem("GraphView/Reset or Create Asset")]
    public static void ResetOrCreateAsset()
    {
        MathBook book = Create();

        AssetDatabase.CreateAsset(book, GetAssetPath());

        for (int i = 0; i < book.nodes.Count; ++i)
        {
            AssetDatabase.AddObjectToAsset(book.nodes[i], book);
        }

        for (int i = 0; i < book.stickyNotes.Count; ++i)
        {
            AssetDatabase.AddObjectToAsset(book.stickyNotes[i], book);
        }

        for (int i = 0; i < book.placemats.Count; ++i)
        {
            AssetDatabase.AddObjectToAsset(book.placemats[i], book);
        }

        AssetDatabase.SaveAssets();
    }

    public static MathBook Create()
    {
        var book = ScriptableObject.CreateInstance<MathBook>();
        book.Clear();

        var equation1 = ScriptableObject.CreateInstance<MathResult>();

        float yOffset = 0;
        {
            equation1.m_Position = new Vector2(1100, yOffset);

            var constant1 = ScriptableObject.CreateInstance<MathConstant>();
            constant1.m_Value = 5;
            constant1.m_Position = new Vector2(0, yOffset);

            var constant2 = ScriptableObject.CreateInstance<MathConstant>();
            constant2.m_Value = 3;
            constant2.m_Position = new Vector2(0, yOffset + 100);

            var constant3 = ScriptableObject.CreateInstance<MathConstant>();
            constant3.m_Value = 2;
            constant3.m_Position = new Vector2(0, yOffset + 200);

            var add1 = ScriptableObject.CreateInstance<MathAdditionOperator>();
            add1.m_Position = new Vector2(700, yOffset);

            var add2 = ScriptableObject.CreateInstance<MathAdditionOperator>();
            add2.m_Position = new Vector2(350, yOffset + 100);

            var note = ScriptableObject.CreateInstance<MathStickyNote>();
            note.position = new Rect(new Vector2(265, yOffset - 175), new Vector2(350, 150));
            note.title = "A Note on Scopes, Groups and Placemats";
            note.contents = "Scopes and Groups are a distinct concepts from Placemats. They serve a similar purpose, which is to " +
                "group elements together. However, they are not meant to be used together. You should choose " +
                "either one or the other in your implementation.\n\nTo that effect, they have not been tested in " +
                "conjunction and no guarantee is made they work with one another.";

            book.Add(equation1);
            book.Add(constant1);
            book.Add(constant2);
            book.Add(constant3);
            book.Add(add1);
            book.Add(add2);
            book.Add(note);

            add2.left = constant2;
            add2.right = constant3;
            add1.left = constant1;
            add1.right = add2;

            equation1.root = add1;
        }

        return book;
    }
}
