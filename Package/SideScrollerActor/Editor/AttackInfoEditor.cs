using UnityEngine;
using UnityEditor;
using KahaGameCore.Package.SideScrollerActor.WeaponScripts;

[CustomEditor(typeof(AttackInfo))]
public class AttackInfoEditor : Editor
{
    private void OnSceneGUI()
    {
        AttackInfo attackInfo = (AttackInfo)target;

        if (attackInfo.enableAreaDamage && attackInfo.explosionRadius > 0)
        {
            // 繪製爆炸範圍圓圈
            Handles.color = new Color(1, 0, 0, 0.3f); // 半透明紅色
            Handles.DrawSolidDisc(attackInfo.transform.position, Vector3.forward, attackInfo.explosionRadius);

            // 繪製爆炸範圍輪廓
            Handles.color = Color.red;
            Handles.DrawWireDisc(attackInfo.transform.position, Vector3.forward, attackInfo.explosionRadius);

            // 添加標籤
            Handles.Label(attackInfo.transform.position + Vector3.up * attackInfo.explosionRadius,
                $"爆炸半徑: {attackInfo.explosionRadius}m");
        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        AttackInfo attackInfo = (AttackInfo)target;

        if (attackInfo.enableAreaDamage)
        {
            EditorGUILayout.HelpBox("爆炸範圍將在場景視圖中顯示。", MessageType.Info);

            // 添加一個按鈕來測試爆炸範圍
            if (GUILayout.Button("預覽爆炸範圍"))
            {
                SceneView.RepaintAll();
            }
        }

        // 如果修改了屬性，重繪場景視圖
        if (GUI.changed)
        {
            SceneView.RepaintAll();
        }
    }
}