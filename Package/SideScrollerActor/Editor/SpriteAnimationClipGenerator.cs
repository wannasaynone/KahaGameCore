using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class SpriteAnimationClipGenerator : EditorWindow
{
    private string folderPath = string.Empty;
    private float frameRate = 12f;
    private bool loopAnimation = true;

    [MenuItem("Tools/Sprite Animation Clip Generator")]
    public static void ShowWindow()
    {
        GetWindow<SpriteAnimationClipGenerator>("Animation Clip Generator");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("1) 選擇存放 Sprite 的資料夾：");
        if (GUILayout.Button("選擇資料夾"))
        {
            // 開啟資料夾選擇視窗
            string selectedPath = EditorUtility.OpenFolderPanel("Select Folder With Sprites", "", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                // 將絕對路徑轉成相對於 Assets 的路徑
                if (selectedPath.StartsWith(Application.dataPath))
                {
                    folderPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                }
                else
                {
                    // 如果不在 Assets 內，無法直接使用 AssetDatabase
                    Debug.LogError($"所選資料夾必須位於 Unity 的 Assets 目錄下：{selectedPath}");
                }
            }
        }

        EditorGUILayout.LabelField("資料夾位置:", folderPath);

        EditorGUILayout.Space();

        frameRate = EditorGUILayout.FloatField("Frame Rate:", frameRate);
        loopAnimation = EditorGUILayout.Toggle("Loop Animation:", loopAnimation);

        EditorGUILayout.Space();

        // 產生 Animation Clips
        if (GUILayout.Button("產生 Animation Clips"))
        {
            CreateAnimationClips(folderPath);
        }
    }

    private void CreateAnimationClips(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("尚未選擇資料夾。");
            return;
        }

        // 搜尋該資料夾下所有 PNG 文件（你可以改成其它副檔名或增加搜尋的類型）
        string[] files = Directory.GetFiles(path, "*.png", SearchOption.TopDirectoryOnly);

        if (files.Length == 0)
        {
            Debug.LogWarning($"找不到任何 PNG 文件於：{path}");
            return;
        }

        // 遍歷每一個檔案並建立對應的 AnimationClip
        foreach (string file in files)
        {
            // 將絕對路徑轉換回相對於 Assets 的路徑
            string assetPath = file.Replace(Application.dataPath, "Assets");

            // 載入該檔案下的所有子 Sprite
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                                           .OfType<Sprite>()
                                           .OrderBy(s => s.name, new NaturalStringComparer()) // 排序：確保名稱順序的一致性
                                           .ToArray();

            if (sprites.Length <= 1)
            {
                // 若只有一張或沒有，通常就不需要製作成動畫
                Debug.LogWarning($"{assetPath} - 未偵測多張 Sprite，跳過建立動畫。");
                continue;
            }

            // 創建一個新的 AnimationClip
            AnimationClip clip = new AnimationClip();
            clip.frameRate = frameRate;

            // 設定動畫是否循環
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loopAnimation;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            // 綁定 SpriteRenderer 的屬性
            EditorCurveBinding spriteBinding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };

            // 設定關鍵影格
            ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[sprites.Length];
            float timePerFrame = 1f / frameRate;
            for (int i = 0; i < sprites.Length; i++)
            {
                keyFrames[i] = new ObjectReferenceKeyframe
                {
                    time = i * timePerFrame,
                    value = sprites[i]
                };
            }

            // 將關鍵影格套用至 clip
            AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyFrames);

            // 用檔名作為 clip 名稱
            string fileName = Path.GetFileNameWithoutExtension(file);
            string clipPath = Path.Combine(path, fileName + ".anim");
            clipPath = clipPath.Replace(Application.dataPath, "Assets");

            // 產生 AnimationClip Asset
            AssetDatabase.CreateAsset(clip, clipPath);
            Debug.Log($"已建立 Animation Clip：{clipPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("所有 Animation Clips 皆已完成產生！");
    }

    // 針對字串自然排序的比較器，以在名字中帶有數字時能夠順序更自然。
    public class NaturalStringComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            return EditorUtility.NaturalCompare(x, y);
        }
    }
}