using UnityEngine;
using UnityEditor.Animations;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class AnimationAndAnimatorTool : EditorWindow
{
    private string folderPath = "Assets/Animations";

    [MenuItem("Tools/Animation and Animator Generator")]
    public static void ShowWindow()
    {
        GetWindow<AnimationAndAnimatorTool>("Animation & Animator Generator");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("指定含有 .anim 檔的資料夾路徑：", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        folderPath = EditorGUILayout.TextField("Folder Path", folderPath);
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

        if (GUILayout.Button("開始生成 Animator"))
        {
            GenerateAnimator(folderPath);
        }
    }

    private void GenerateAnimator(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            Debug.LogError("指定的路徑不是有效的資料夾： " + path);
            return;
        }

        // 1. 搜尋資料夾內所有的 AnimationClips
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { path });
        if (guids.Length == 0)
        {
            Debug.LogWarning("資料夾內沒有任何 AnimationClip.");
            return;
        }

        List<AnimationClip> clips = new List<AnimationClip>();

        foreach (var guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);

            // 2. 重新命名：將 "SpriteSheet_{animationName}" 轉成 "{animationName}"
            if (clip != null && clip.name.StartsWith("SpriteSheet_"))
            {
                string newName = clip.name.Replace("SpriteSheet_", "");
                // 重新命名檔案以及資產名稱
                string newPath = Path.GetDirectoryName(assetPath) + "/" + newName + ".anim";
                AssetDatabase.RenameAsset(assetPath, newName);
                AssetDatabase.MoveAsset(assetPath, newPath);
                AssetDatabase.SaveAssets();

                // 重新載入已改名後的 clip
                clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(newPath);
            }

            if (clip != null)
            {
                clips.Add(clip);
            }
        }

        // 3. 新建 Animator Controller
        string controllerPath = Path.Combine(path, "GeneratedController.controller");
        AnimatorController animatorController = null;
        if (File.Exists(controllerPath))
        {
            // 若已存在，則重新載入
            animatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        }
        else
        {
            animatorController = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        }

        // 4. 建立對應的 Animator State
        AnimatorStateMachine rootStateMachine = animatorController.layers[0].stateMachine;

        // 先清除舊的 state（防止重複生成）
        var states = rootStateMachine.states;
        for (int i = 0; i < states.Length; i++)
        {
            rootStateMachine.RemoveState(states[i].state);
        }

        bool hasIdle = false;
        AnimatorState idleState = null;
        foreach (var clip in clips)
        {
            AnimatorState state = rootStateMachine.AddState(clip.name);
            state.motion = clip;

            if (clip.name.ToLower() == "idle")
            {
                hasIdle = true;
                idleState = state;
            }
        }

        // 若有 Idle，將它設定為預設 State
        if (hasIdle && idleState != null)
        {
            rootStateMachine.defaultState = idleState;
        }
        else
        {
            // 若沒有 Idle，就建立一個空的預設 State
            AnimatorState defaultState = rootStateMachine.AddState("DefaultEmptyState");
            rootStateMachine.defaultState = defaultState;
        }

        // 5. 刷新資產資料庫
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("已完成 Animator 生成！路徑：" + controllerPath);
    }
}