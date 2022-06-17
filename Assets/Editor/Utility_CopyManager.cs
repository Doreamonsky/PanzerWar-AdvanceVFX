using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Utility_CopyManager : EditorWindow
{
    private static List<string> copyBuffers = new List<string>();

    private static EditorWindow win;

    [MenuItem("GameObject/Open Copy Manager", false, -10)]

    private static void CreateWindow()
    {
        win = EditorWindow.GetWindow(typeof(Utility_CopyManager));
        win.titleContent.text = "Copy Manager";
    }

    private void OnGUI()
    {
        if (copyBuffers.Count != 0)
        {
            if (GUILayout.Button("Clean Buffers"))
            {
                copyBuffers = new List<string>();
            }
        }

        foreach (var copyBuffer in copyBuffers)
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Copy"))
            {
                GUIUtility.systemCopyBuffer = copyBuffer;
                GUILayout.Space(10);
            }

            EditorGUILayout.LabelField(copyBuffer);

            EditorGUILayout.EndHorizontal();
        }
    }


    [MenuItem("GameObject/Copy Relative Path", false, -10)]
    private static void CopyRelativePath()
    {
        var selection = Selection.activeObject as GameObject;

        if (selection != null)
        {
            var parent = selection.transform.parent;
            var path = selection.name;

            while (parent.parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            GUIUtility.systemCopyBuffer = path;
            copyBuffers.Add(path);

            if (win == null)
            {
                CreateWindow();
            }
        }
    }
}
