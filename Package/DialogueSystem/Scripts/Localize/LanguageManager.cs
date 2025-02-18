using KahaGameCore.GameData.Implemented;
using UnityEngine;

namespace KahaGameCore.Package.DialogueSystem
{
    public enum Language
    {
        en_us,
        zh_tw,
        ja_jp,
    }

    public class LanguageManager
    {
        public static LanguageManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new LanguageManager();
                }
                return instance;
            }
        }
        private static LanguageManager instance;

        public GameStaticDataManager GameStaticDataManager { get; private set; }
        public Language CurrentLanguage { get; private set; }

        public event System.Action<Language> OnLanguageChanged;

        private LanguageManager()
        {
            GameStaticDataManager = new GameStaticDataManager();
            GameStaticDataDeserializer gameStaticDataDeserializer = new GameStaticDataDeserializer();

            TextAsset textAsset = Resources.Load<TextAsset>("GameData/LocalizeData");

            if (textAsset == null)
            {
                Debug.LogError("[LanguageManager] Can't find LocalizeData in Resources/GameData/LocalizeData.");
                return;
            }

            GameStaticDataManager.Add<LocalizeData>(gameStaticDataDeserializer.Read<LocalizeData[]>(textAsset.text));

            CurrentLanguage = Language.en_us;
        }

        public void ChangeLanguage(int language)
        {
            ChangeLanguage((Language)language);
        }

        public void ChangeLanguage(Language language)
        {
            CurrentLanguage = language;
            OnLanguageChanged?.Invoke(CurrentLanguage);
        }
    }
}
