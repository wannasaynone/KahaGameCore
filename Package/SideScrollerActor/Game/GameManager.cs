using System;
using System.Collections.Generic;
using KahaGameCore.GameData.Implemented;
using KahaGameCore.GameEvent;
using KahaGameCore.Package.SideScrollerActor.Data;
using KahaGameCore.Package.SideScrollerActor.InGameEvent;
using KahaGameCore.Package.SideScrollerActor.Utlity;
using KahaGameCore.Package.SideScrollerActor.View;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Game
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private BootView bootView;
        [SerializeField] private TitleView titleView;
        [SerializeField] private DialogueSystem.DialogueView dialogueView;
        [SerializeField] private InGameView ingameView;
        [SerializeField] private GameEndView gameEndView;
        [Header("Data Source")]
        [SerializeField] private TextAsset dialogueData;
        [SerializeField] private TextAsset itemData;
        [SerializeField] private TextAsset contextData;

        private GameStaticDataManager gameStaticDataManager;

        private List<OnItemRecordChanged> itemRecord;

        private void Awake()
        {
            gameStaticDataManager = new GameStaticDataManager();
            GameStaticDataDeserializer gameStaticDataDeserializer = new GameStaticDataDeserializer();
            gameStaticDataManager.Add<DialogueSystem.DialogueData>(gameStaticDataDeserializer.Read<DialogueSystem.DialogueData[]>(dialogueData.text));
            gameStaticDataManager.Add<ItemData>(gameStaticDataDeserializer.Read<ItemData[]>(itemData.text));
            gameStaticDataManager.Add<ContextData>(gameStaticDataDeserializer.Read<ContextData[]>(contextData.text));

            DialogueSystem.PlayerManager.Initialize();
            DialogueSystem.PlayerManager.Instance.Player.LoadDataFromSaveField();

            Cutscene.CutscenePlayer.Initialize(dialogueView);

            ContextHandler.Initialize(gameStaticDataManager);
            ContextHandler.Instance.OnLanguageChanged += OnLanguageChanged;

            DialogueSystem.DialogueManager.Initialize(gameStaticDataManager, new DialogueSystemExtend.DialogueCommandFactory(DialogueSystem.PlayerManager.Instance.Player), GetLanguageTypeByContextHandler());

            Invoke(nameof(OnGameInitialized), 0.1f);
        }

        private DialogueSystem.DialogueManager.LanguageType GetLanguageTypeByContextHandler()
        {
            switch (ContextHandler.Instance.CurrentLanguage)
            {
                case ContextHandler.LanguageType.zh_tw:
                    return DialogueSystem.DialogueManager.LanguageType.TranditionalChinese;
                case ContextHandler.LanguageType.en_us:
                    return DialogueSystem.DialogueManager.LanguageType.English;
                case ContextHandler.LanguageType.zh_hans:
                    return DialogueSystem.DialogueManager.LanguageType.SimplifiedChinese;
                case ContextHandler.LanguageType.ja_jp:
                    return DialogueSystem.DialogueManager.LanguageType.Japanese;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnLanguageChanged()
        {
            DialogueSystem.DialogueManager.Instance.currentLanguage = GetLanguageTypeByContextHandler();
        }

        private void OnGameInitialized()
        {
            EventBus.Subscribe<TitleView_OnStartGameRequested>(OnTitleViewStartGameRequested);
            bootView.StartShow();
        }

        private void Update()
        {
            Application.targetFrameRate = 60;

            if (Input.GetKeyUp(KeyCode.Y))
            {
                ContextHandler.Instance.CurrentLanguage = ContextHandler.LanguageType.zh_tw;
            }
            if (Input.GetKeyUp(KeyCode.U))
            {
                ContextHandler.Instance.CurrentLanguage = ContextHandler.LanguageType.en_us;
            }
            if (Input.GetKeyUp(KeyCode.I))
            {
                ContextHandler.Instance.CurrentLanguage = ContextHandler.LanguageType.zh_hans;
            }
            if (Input.GetKeyUp(KeyCode.O))
            {
                ContextHandler.Instance.CurrentLanguage = ContextHandler.LanguageType.ja_jp;
            }
        }

        private void OnTitleViewStartGameRequested(TitleView_OnStartGameRequested obj)
        {
            EventBus.Unsubscribe<TitleView_OnStartGameRequested>(OnTitleViewStartGameRequested);
            StartGame(obj.sequenceName);
        }

        private void StartGame(string startSequenceName)
        {
            itemRecord = new List<OnItemRecordChanged>();
            List<DialogueSystem.Player.OwingItemData> owingItemDatas = DialogueSystem.PlayerManager.Instance.Player.GetOwingItemDatas();

            foreach (var item in owingItemDatas)
            {
                itemRecord.Add(new OnItemRecordChanged()
                {
                    itemID = item.id,
                    currentAmount = item.count
                });
            }

            GameState_Combat gameState_combat = new GameState_Combat(ingameView, gameEndView, titleView, itemRecord, gameStaticDataManager);
            gameState_combat.Start(OnGameEnded);

            EventBus.Publish(new Game_TriggerSequence
            {
                sequenceName = startSequenceName
            });
        }

        private void OnGameEnded()
        {
            EventBus.Subscribe<TitleView_OnStartGameRequested>(OnTitleViewStartGameRequested);
            titleView.gameObject.SetActive(true);
            GeneralBlackScreen.Instance.FadeOut(null);
        }
    }
}