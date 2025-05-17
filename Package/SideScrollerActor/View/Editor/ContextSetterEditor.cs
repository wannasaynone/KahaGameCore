using KahaGameCore.GameData.Implemented;
using KahaGameCore.Package.SideScrollerActor.Utlity;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.View.Editor
{
    [CustomEditor(typeof(UserInterfaceContextSetter))]
    public class ContextSetterEditor : UnityEditor.Editor
    {
        private ContextHandler contextHandler;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            UserInterfaceContextSetter contextSetter = (UserInterfaceContextSetter)target;

            SerializedObject serializedObject = new SerializedObject(contextSetter);
            SerializedProperty contextID = serializedObject.FindProperty("contextID");

            if (contextHandler == null)
            {
                GameStaticDataManager gameStaticDataManager = new GameStaticDataManager();
                GameStaticDataDeserializer gameStaticDataDeserializer = new GameStaticDataDeserializer();
                gameStaticDataManager.Add<Data.ContextData>(gameStaticDataDeserializer.Read<Data.ContextData[]>(Resources.Load<TextAsset>("Data/ContextData").text));

                contextHandler = new ContextHandler(gameStaticDataManager);
            }

            string contextText = contextHandler.GetContext(contextID.intValue);

            if (string.IsNullOrEmpty(contextText))
            {
                EditorGUILayout.LabelField("Context Text", "NULL");
                return;
            }

            EditorGUILayout.LabelField("Context Text", contextText, EditorStyles.wordWrappedLabel);
        }
    }
}