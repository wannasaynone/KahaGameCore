using KahaGameCore.GameData.Implemented;
using UnityEditor;
using UnityEngine;
using KahaGameCore.Package.SideScrollerActor.Level.InteractableObject;
using KahaGameCore.Package.SideScrollerActor.Data;

namespace KahaGameCore.Package.SideScrollerActor.Level
{
    [CustomEditor(typeof(InteractableObject_SimpleCollect)), CanEditMultipleObjects]
    public class ContextSetterEditor : Editor
    {
        private GameStaticDataManager gameStaticDataManager;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            InteractableObject_SimpleCollect contextSetter = (InteractableObject_SimpleCollect)target;

            SerializedObject serializedObject = new SerializedObject(contextSetter);
            SerializedProperty contextID = serializedObject.FindProperty("itemId");

            if (gameStaticDataManager == null)
            {
                gameStaticDataManager = new GameStaticDataManager();
                GameStaticDataDeserializer gameStaticDataDeserializer = new GameStaticDataDeserializer();
                gameStaticDataManager.Add<ItemData>(gameStaticDataDeserializer.Read<ItemData[]>(Resources.Load<TextAsset>("Data/ItemData").text));
            }

            ItemData itemData = gameStaticDataManager.GetGameData<ItemData>(contextID.intValue);


            if (itemData == null)
            {
                EditorGUILayout.LabelField("Item: ", "NULL");
                return;
            }

            string contextText = itemData.Name + " " + itemData.ItemType;

            HorizontalLine();

            EditorGUILayout.LabelField("Item: ", contextText);
        }

        private void HorizontalLine()
        {
            EditorGUILayout.Space();
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
            EditorGUILayout.Space();
        }
    }
}