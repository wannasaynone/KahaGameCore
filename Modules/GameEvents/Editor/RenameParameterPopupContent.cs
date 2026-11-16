using System;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.GameEvents.Editor
{
    internal sealed class RenameParameterPopupContent : PopupWindowContent
    {
        private const string KeyFieldName = "RenameParameterKeyField";

        private readonly string oldKey;
        private readonly Func<string, string> rename;
        private string newKey;
        private string error;
        private bool focused;

        /// <param name="rename">回傳錯誤訊息；成功時回傳 null。</param>
        public RenameParameterPopupContent(string oldKey, Func<string, string> rename)
        {
            this.oldKey = oldKey ?? string.Empty;
            this.rename = rename ?? throw new ArgumentNullException(nameof(rename));
            newKey = this.oldKey;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(360f, string.IsNullOrEmpty(error) ? 130f : 200f);
        }

        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.LabelField("參數鍵改名", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "會立即儲存參數表，並改寫目前事件目錄的事件，以及所有場景／prefab 公式中的 $參數鍵。",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("原參數鍵", oldKey);
            GUI.SetNextControlName(KeyFieldName);
            newKey = EditorGUILayout.TextField("新參數鍵", newKey ?? string.Empty);
            if (!focused)
            {
                EditorGUI.FocusTextInControl(KeyFieldName);
                focused = true;
            }

            if (!string.IsNullOrEmpty(error))
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }

            bool submit = Event.current.type == EventType.KeyDown &&
                (Event.current.keyCode == KeyCode.Return ||
                 Event.current.keyCode == KeyCode.KeypadEnter);

            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("取消", GUILayout.Width(72f)))
                {
                    editorWindow.Close();
                    return;
                }

                submit |= GUILayout.Button("改名", GUILayout.Width(72f));
            }

            if (submit)
            {
                error = rename(newKey);
                if (error == null)
                {
                    editorWindow.Close();
                    return;
                }

                Event.current.Use();
            }
        }
    }
}
