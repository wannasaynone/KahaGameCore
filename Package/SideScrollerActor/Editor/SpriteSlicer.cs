using UnityEngine;
using UnityEditor;
using System;
using System.IO;

public class SpriteSlicer : EditorWindow
{
    // 預設資料夾路徑，可自行修改
    private string folderPath = "Assets/Sprites";
    // 預設 X/Y 切割大小（單位：像素）
    private int sliceX = 32;
    private int sliceY = 32;

    [MenuItem("Tools/Sprite Slicer")]
    static void ShowWindow()
    {
        SpriteSlicer window = GetWindow<SpriteSlicer>("Sprite Slicer");
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("設定資料夾及切割大小", EditorStyles.boldLabel);

        // 選擇資料夾
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Folder Path:", GUILayout.Width(80));
        folderPath = EditorGUILayout.TextField(folderPath);
        if (GUILayout.Button("瀏覽資料夾", GUILayout.Width(100)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("選擇資料夾", Application.dataPath, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                // 轉換成相對路徑
                if (selectedPath.StartsWith(Application.dataPath))
                {
                    folderPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                }
                else
                {
                    folderPath = selectedPath;
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        // X/Y 切割大小
        sliceX = EditorGUILayout.IntField("Grid Size X:", sliceX);
        sliceY = EditorGUILayout.IntField("Grid Size Y:", sliceY);

        GUILayout.Space(10);
        if (GUILayout.Button("開始自動切割"))
        {
            SliceSprites();
        }
    }

    private void SliceSprites()
    {
        if (!Directory.Exists(folderPath))
        {
            Debug.LogError($"資料夾不存在：{folderPath}");
            return;
        }

        // 取得資料夾內所有圖片檔案（可自行修正副檔名條件）
        string[] files = Directory.GetFiles(folderPath, "*.png", SearchOption.AllDirectories);
        if (files.Length == 0)
        {
            Debug.LogWarning("未找到任何 png 檔案。");
            return;
        }

        foreach (var file in files)
        {
            // 取得 TextureImporter
            var importer = AssetImporter.GetAtPath(file) as TextureImporter;
            if (importer == null) continue;

            // 設定為 Sprite / Multiple
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;

            // 載入圖片以計算大小
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(file);
            if (tex == null)
            {
                Debug.LogError($"無法載入圖片：{file}");
                continue;
            }

            int width = tex.width;
            int height = tex.height;

            // 計算要切割幾塊
            int colCount = width / sliceX;
            int rowCount = height / sliceY;
            if (colCount == 0 || rowCount == 0)
            {
                Debug.LogWarning($"檔案：{file} 尺寸不符合設定的切割大小 ({sliceX}x{sliceY})。");
                continue;
            }

            // 建立 SpriteMetaData 陣列
            SpriteMetaData[] metas = new SpriteMetaData[colCount * rowCount];
            int index = 0;
            for (int y = 0; y < rowCount; y++)
            {
                for (int x = 0; x < colCount; x++)
                {
                    SpriteMetaData meta = new SpriteMetaData();
                    meta.rect = new Rect(x * sliceX, y * sliceY, sliceX, sliceY);
                    // Unity 以左下為原點，需要設定 pivot
                    meta.pivot = new Vector2(0.5f, 0.5f);
                    meta.alignment = (int)SpriteAlignment.Center;
                    meta.name = $"{Path.GetFileNameWithoutExtension(file)}_{y}_{x}";
                    metas[index++] = meta;
                }
            }

            // 設定切圖資訊
#pragma warning disable CS0618 // Type or member is obsolete
            importer.spritesheet = metas;
#pragma warning restore CS0618 // Type or member is obsolete
            importer.spritePixelsPerUnit = 100;
            AssetDatabase.WriteImportSettingsIfDirty(file);
            importer.SaveAndReimport();
            Debug.Log($"成功切割 Sprite：{file}");
        }

        AssetDatabase.Refresh();
        Debug.Log("所有圖片皆已成功切割完成！");
    }
}