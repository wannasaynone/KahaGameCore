using KahaGameCore.Package.DialogueSystem;
using UnityEditor;
using UnityEngine;

namespace ProjectASNIM
{
    public class DebugTool : EditorWindow
    {
        private static DebugTool window;
        private int addItemID;


        [MenuItem("Tools/Debug Tool")]
        public static void ShowWindow()
        {
            window = GetWindow<DebugTool>();
            window.titleContent = new GUIContent("Debug Tool");
            window.Show();
        }

        private void AddHorizontalLine()
        {
            GUILayout.Space(10);

            GUIStyle horizontalLine;
            horizontalLine = new GUIStyle();
            horizontalLine.normal.background = EditorGUIUtility.whiteTexture;
            horizontalLine.margin = new RectOffset(0, 0, 4, 4);
            horizontalLine.fixedHeight = 1;

            GUILayout.Box(GUIContent.none, horizontalLine);

            GUILayout.Space(10);
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Delete Save"))
            {

            }

            if (GUILayout.Button("Open Save Folder"))
            {
                string path = Application.persistentDataPath;
                System.Diagnostics.Process.Start(path);
            }

            AddHorizontalLine();

            GUILayout.Label("Add Item", EditorStyles.boldLabel);
            addItemID = EditorGUILayout.IntField("Item ID", addItemID);
            if (GUILayout.Button("Add Item"))
            {
                PlayerManager.Instance.Player.AddItem(addItemID, 1);
            }
        }
    }
}