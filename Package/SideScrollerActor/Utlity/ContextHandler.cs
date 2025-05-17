using System;
using KahaGameCore.GameData.Implemented;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Utlity
{
    public class ContextHandler
    {
        public static ContextHandler Instance
        {
            get
            {
                if (instance == null)
                {
                    Debug.LogError("ContextHandler is not initialized. Please call Initialize() method before using it.");
                    return null;
                }
                return instance;
            }
        }
        private static ContextHandler instance;
        public static void Initialize(GameStaticDataManager gameStaticDataManager)
        {
            if (instance != null)
            {
                Debug.LogError("ContextHandler is already initialized. Please use the existing instance.");
                return;
            }

            instance = new ContextHandler(gameStaticDataManager);

            switch (Application.systemLanguage)
            {
                case SystemLanguage.ChineseTraditional:
                    instance.CurrentLanguage = LanguageType.zh_tw;
                    break;
                case SystemLanguage.English:
                    instance.CurrentLanguage = LanguageType.en_us;
                    break;
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                    instance.CurrentLanguage = LanguageType.zh_hans;
                    break;
                case SystemLanguage.Japanese:
                    instance.CurrentLanguage = LanguageType.ja_jp;
                    break;
                default:
                    instance.CurrentLanguage = LanguageType.en_us;
                    break;
            }
        }

        private readonly GameStaticDataManager gameStaticDataManager;

        public event Action OnLanguageChanged;
        public enum LanguageType
        {
            zh_tw,
            en_us,
            zh_hans,
            ja_jp
        }
        private LanguageType currentLanguage = LanguageType.zh_tw;
        public LanguageType CurrentLanguage
        {
            get => currentLanguage;
            set
            {
                if (currentLanguage != value)
                {
                    currentLanguage = value;
                    Debug.Log($"Language changed to: {currentLanguage}");
                    OnLanguageChanged?.Invoke();
                }
            }
        }

        public ContextHandler(GameStaticDataManager gameStaticDataManager)
        {
            this.gameStaticDataManager = gameStaticDataManager;
        }

        public string GetContext(int contextID)
        {
            Data.ContextData contextData = gameStaticDataManager.GetGameData<Data.ContextData>(contextID);

            if (contextData == null)
            {
                Debug.LogError($"ContextData with ID {contextID} not found.");
                return "NULL";
            }

            switch (currentLanguage)
            {
                case LanguageType.zh_tw:
                    return contextData.zh_tw;
                case LanguageType.en_us:
                    return contextData.en_us;
                case LanguageType.zh_hans:
                    return contextData.zh_hans;
                case LanguageType.ja_jp:
                    return contextData.ja_jp;
                default:
                    return "NULL";
            }
        }
    }
}