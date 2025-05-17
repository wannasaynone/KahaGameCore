using UnityEngine;
using UnityEditor;

namespace KahaGameCore.Package.SideScrollerActor.Gameplay
{
    [CustomPropertyDrawer(typeof(ObserverableAttribute))]
    public class ObserverableDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // 保存當前的 GUI 狀態
            bool previousGUIState = GUI.enabled;

            // 設置 GUI 為禁用狀態
            GUI.enabled = false;

            // 繪製默認的屬性字段
            EditorGUI.PropertyField(position, property, label);

            // 恢復之前的 GUI 狀態
            GUI.enabled = previousGUIState;
        }
    }
}