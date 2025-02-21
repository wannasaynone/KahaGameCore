using UnityEngine;
using UnityEditor;

namespace KahaGameCore.Common
{
    [CustomPropertyDrawer(typeof(RenameAttribute))]
    public class RenameEditor : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // 獲取自定義的名稱  
            string newName = (attribute as RenameAttribute).NewName;

            // 如果是 Array 或 List，處理 Foldout 標籤  
            if (property.isArray && property.propertyType == SerializedPropertyType.Generic)
            {
                // 顯示自定義的 Foldout 名稱  
                property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, new GUIContent(newName), true);
            }
            else
            {
                // 處理普通屬性  
                EditorGUI.PropertyField(position, property, new GUIContent(newName), true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // 確保高度正確，特別是對於數組或列表  
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}