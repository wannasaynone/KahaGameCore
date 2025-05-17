using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

public class AnimatorStateTool : EditorWindow
{
    private Animator animator;
    private AnimatorController animatorController;
    private List<AnimatorState> stateList = new List<AnimatorState>();

    private Vector2 scrollPos;

    [MenuItem("Tools/Animator State Tool")]
    public static void ShowWindow()
    {
        GetWindow<AnimatorStateTool>("Animator State Tool");
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // 取得目前選取的GameObject
        GameObject selectedObj = Selection.activeGameObject;
        if (selectedObj == null)
        {
            EditorGUILayout.LabelField("請選取一個GameObject");
            EditorGUILayout.EndScrollView();
            return;
        }

        // 檢查是否有Animator元件
        animator = selectedObj.GetComponent<Animator>();
        if (animator == null)
        {
            EditorGUILayout.LabelField("選取的GameObject沒有Animator");
            EditorGUILayout.EndScrollView();
            return;
        }

        if (animator.runtimeAnimatorController == null)
        {
            EditorGUILayout.LabelField("Animator沒有AnimatorController");
            EditorGUILayout.EndScrollView();
            return;
        }

        // 將runtimeAnimatorController轉型成AnimatorController（僅在Editor中可用）
        animatorController = animator.runtimeAnimatorController as AnimatorController;
        if (animatorController == null)
        {
            EditorGUILayout.LabelField("AnimatorController轉型失敗");
            EditorGUILayout.EndScrollView();
            return;
        }

        // 清除原有列表並獲取所有State（這裡只讀取預設層的狀態）
        stateList.Clear();
        foreach (var layer in animatorController.layers)
        {
            var stateMachine = layer.stateMachine;
            foreach (var childState in stateMachine.states)
            {
                if (childState.state != null)
                    stateList.Add(childState.state);
            }
        }

        EditorGUILayout.LabelField("Animator狀態列表：");
        // 依據每個State建立一個Button，按下後播放該State
        foreach (var state in stateList)
        {
            if (GUILayout.Button(state.name))
            {
                // 呼叫Play播放指定狀態，這裡使用state.name作為狀態名稱
                animator.Play(state.name);
            }
        }

        EditorGUILayout.EndScrollView();
    }
}
