using UnityEngine;

namespace KahaGameCore.EditorTool
{
    [CreateAssetMenu(menuName = "Kaha Game Core/Editor/GoogleSheet2Json Setting")]
    public class GoogleSheet2JsonSetting : ScriptableObject
    {
        public string sheetID;
        public string[] sheetNames;
    }
}