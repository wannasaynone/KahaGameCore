using System.Collections.Generic;
using KahaGameCore.GameEvent;
using UnityEngine;
using KahaGameCore.GameData.Implemented;
using KahaGameCore.Package.SideScrollerActor.InGameEvent;
using KahaGameCore.Package.SideScrollerActor.Gameplay.Controller;
using KahaGameCore.Package.SideScrollerActor.Gameplay;
using KahaGameCore.Package.SideScrollerActor.Data;
using KahaGameCore.Package.SideScrollerActor.Utlity;
using KahaGameCore.Package.SideScrollerActor.Camera;
using KahaGameCore.Package.SideScrollerActor.WeaponScripts;

namespace KahaGameCore.Package.SideScrollerActor.Level
{
    public class LevelManager : MonoBehaviour
    {
        public enum State
        {
            None,
            Initializing,
            Ready,
            Started
        }

        public State CurrentState { get; private set; } = State.None;

        public static int GetItemCount(int itemID)
        {
            if (instance == null)
            {
                return 0;
            }

            OnItemRecordChanged record = instance.itemRecords.Find(i => i.itemID == itemID);

            if (record == null)
            {
                return 0;
            }

            return record.currentAmount;
        }

        private readonly List<object> pauseLocks = new List<object>();
        public static void Pause()
        {
            if (instance == null || instance.CurrentState != State.Started)
            {
                return;
            }

            instance.pauseLocks.Add(new object());
            if (instance.hero != null)
            {
                instance.hero.PauseMove(true);
            }
        }

        public static void Resume()
        {
            if (instance == null || instance.CurrentState != State.Started)
            {
                return;
            }

            if (instance.pauseLocks.Count <= 0)
            {
                Debug.LogError("LevelManager: Resume() called without matching Pause()");
                return;
            }

            instance.pauseLocks.RemoveAt(instance.pauseLocks.Count - 1);
            if (instance.pauseLocks.Count <= 0 && instance.hero != null)
            {
                instance.hero.PauseMove(false);
            }
        }
#if USING_URP
        public static void SetProfile(VolumeProfile profile)
        {
            if (instance == null || instance.CurrentState != State.Started)
            {
                return;
            }

            instance.volume.profile = profile;
        }

        public static VolumeProfile GetCurrentVolumeProfile()
        {
            if (instance == null || instance.CurrentState != State.Started)
            {
                return null;
            }

            return instance.volume.profile;
        }
#endif
        public static GameObject GetSpecialGameObjectByName(string name)
        {
            if (instance == null || instance.CurrentState != State.Started)
            {
                return null;
            }

            GameObject specialGameObject = instance.specialGameObjects.Find(obj => obj.name == name);
            if (specialGameObject == null)
            {
                Debug.LogError($"Special GameObject with name {name} not found. Did you forget to add it to the list in LevelManager?");
                return null;
            }

            return specialGameObject;
        }

        private static LevelManager instance;

        [Header("All Nullable")]
#if USING_URP
        [SerializeField] private Volume volume;
#endif
        [SerializeField] private ActorController actorController;
        [SerializeField] private Actor hero;
        [SerializeField] private RoomSetting startRoom;
        [SerializeField] private List<GameObject> specialGameObjects = new List<GameObject>();

        public enum EndGameType
        {
            NeverEnd,
            AllEnemiesDead
        }

        [SerializeField] private EndGameType endGameType = EndGameType.AllEnemiesDead;

        private GameStaticDataManager gameStaticDataManager;

        [SerializeField] private List<ActorTickerBase> actorTickers = new List<ActorTickerBase>();
        private List<OnItemRecordChanged> itemRecords = new List<OnItemRecordChanged>();
        private WeaponSwitcher weaponSwitcherCache;

        private GameObject aiTickerParent;

        private void OnEnable()
        {
            if (instance != null)
            {
                Debug.LogError("LevelManager is already existed.");
                return;
            }

            instance = this;
            EventBus.Subscribe<InGameItem_OnAmountChanged>(InGameItem_OnAmountChanged);
            EventBus.Subscribe<OnItemRecordChanged>(OnItemRecordChanged);
            EventBus.Subscribe<Game_CallHitPause>(OnGamePause);
        }

        private void InGameItem_OnAmountChanged(InGameItem_OnAmountChanged e)
        {
            if (e.addAmount > 0)
            {
                ItemData itemData = gameStaticDataManager.GetGameData<ItemData>(e.itemID);
                string itemDisplayName = ContextHandler.Instance.GetContext(itemData.NameContextID);
                GeneralHintDisplayer.Instance.Create(itemDisplayName + " +" + e.addAmount, hero.transform.position, Color.yellow);
            }

            OnItemRecordChanged(new OnItemRecordChanged
            {
                itemID = e.itemID,
                currentAmount = GetItemCount(e.itemID) + e.addAmount
            });
        }

        private void OnItemRecordChanged(OnItemRecordChanged e)
        {
            bool isNewItem = false;
            OnItemRecordChanged record = itemRecords.Find(i => i.itemID == e.itemID);

            if (record == null)
            {
                itemRecords.Add(e);
                isNewItem = true;
            }
            else
            {
                record.currentAmount = e.currentAmount;
            }

            ItemData itemData = gameStaticDataManager.GetGameData<ItemData>(e.itemID);
            switch (itemData.ItemType)
            {
                case "Weapon":
                    Weapon weapon = Instantiate(Resources.Load<Weapon>(itemData.PrefabPath), weaponSwitcherCache.transform);
                    weaponSwitcherCache.AddWeapon(weapon);

                    if (isNewItem)
                    {
                        RangeWeapon newRangeWeapon = weapon as RangeWeapon;

                        int ammoID = -1;

                        ItemData[] allItemData = gameStaticDataManager.GetAllGameData<ItemData>();
                        for (int i = 0; i < allItemData.Length; i++)
                        {
                            if (allItemData[i].Name == itemData.Name && allItemData[i].ItemType == "Ammo")
                            {
                                ammoID = allItemData[i].ID;
                                break;
                            }
                        }

                        if (newRangeWeapon != null)
                        {
                            EventBus.Publish(new OnItemRecordChanged()
                            {
                                itemID = ammoID,
                                currentAmount = newRangeWeapon.MaxAmmo
                            });
                        }
                    }

                    break;
                case "Ammo":
                    RangeWeapon rangeWeapon = weaponSwitcherCache.GetWeapon(itemData.Name) as RangeWeapon;
                    if (rangeWeapon != null)
                    {
                        rangeWeapon.SetRemainingAmmo(e.itemID, e.currentAmount);
                    }
                    break;
            }
        }

        private void OnDisable()
        {
            if (instance == this)
            {
                instance = null;
                EventBus.Unsubscribe<OnItemRecordChanged>(OnItemRecordChanged);
                EventBus.Unsubscribe<Game_CallHitPause>(OnGamePause);
                EventBus.Unsubscribe<InGameItem_OnAmountChanged>(InGameItem_OnAmountChanged);
            }
        }

        private float remainingPauseTime = -1f;
        private void OnGamePause(Game_CallHitPause e)
        {
            if (remainingPauseTime <= 0f)
            {
                Time.timeScale = 0.05f;
                remainingPauseTime = e.duration;
                StartCoroutine(IEGamePause());
            }
            else
            {
                remainingPauseTime += e.duration;
            }
        }

        private System.Collections.IEnumerator IEGamePause()
        {
            while (remainingPauseTime > 0)
            {
                remainingPauseTime -= Time.unscaledDeltaTime;
                yield return null;
            }

            Time.timeScale = 1;
        }

        public void Initialize(List<OnItemRecordChanged> itemRecords, GameStaticDataManager gameStaticDataManager, int heroHealth, float heroStamina)
        {
            if (CurrentState != State.None)
            {
                Debug.LogError("LevelManager is running.");
                return;
            }

            this.itemRecords = itemRecords;
            StartCoroutine(IEInitialize(itemRecords, gameStaticDataManager, heroHealth, heroStamina));
        }

        private void OnDestroy()
        {
            ActorContainer.ClearAll();
        }

        private System.Collections.IEnumerator IEInitialize(List<OnItemRecordChanged> itemRecords, GameStaticDataManager gameStaticDataManager, int heroHealth, float heroStamina)
        {
            CurrentState = State.Initializing;

            this.gameStaticDataManager = gameStaticDataManager;

            if (hero != null)
            {
                yield return IEInitializeHero(itemRecords, heroHealth, heroStamina);
            }

            yield return null;

            List<Actor> enemies = ActorContainer.GetActorsByCamp(Actor.Camp.Monster);

            for (int i = 0; i < enemies.Count; i++)
            {
                Weapon defaultWeapon = enemies[i].GetComponentInChildren<Weapon>();
                enemies[i].Initialize(defaultWeapon);
            }

            yield return null;

            CurrentState = State.Ready;
        }

        public void SimpleAddActorTicker<T>() where T : ActorTickerBase
        {
            List<Actor> enemies = ActorContainer.GetActorsByCamp(Actor.Camp.Monster);

            for (int i = 0; i < enemies.Count; i++)
            {
                if (actorTickers.Find(ticker => ticker.IsControling(enemies[i])) == null)
                {
                    ActorTickerBase monsterTicker = new GameObject("MonsterTicker").AddComponent<T>();
                    monsterTicker.Bind(enemies[i]);
                    actorTickers.Add(monsterTicker);

                    if (aiTickerParent == null)
                    {
                        aiTickerParent = new GameObject("[AITickerParent]");
                        aiTickerParent.transform.SetParent(transform);
                    }

                    monsterTicker.transform.SetParent(aiTickerParent.transform);
                    monsterTicker.transform.localPosition = Vector3.zero;
                    monsterTicker.transform.localRotation = Quaternion.identity;
                    monsterTicker.transform.localScale = Vector3.one;
                }
            }
        }

        private System.Collections.IEnumerator IEInitializeHero(List<OnItemRecordChanged> itemRecords, int heroHealth, float heroStamina)
        {
            weaponSwitcherCache = hero.GetComponentInChildren<WeaponSwitcher>();

            if (weaponSwitcherCache != null)
            {
                yield return IEInitializeWeaponSwitcher(itemRecords);
            }

            hero.camp = Actor.Camp.Hero;
            hero.Initialize(weaponSwitcherCache.GetDefaultWeapon());

            CameraController.Instance.target = hero.transform;
            BoardSetter.SetBoard(startRoom.BoardTransform_min.position.x, startRoom.BoardTransform_max.position.x);
            hero.ForceUpdateCameraSettingWithThisActor();
            CameraController.Instance.SetToTargetPositionImmediately();



            actorController.SetControlTarget(hero);

            yield return null;

            if (heroHealth > 0)
            {
                hero.currentHealth = heroHealth;
            }

            if (heroStamina > 0)
            {
                hero.currentStamina = heroStamina;
            }

            hero.PauseMove(true);
        }

        private System.Collections.IEnumerator IEInitializeWeaponSwitcher(List<OnItemRecordChanged> itemRecords)
        {
            List<RangeWeapon> rangeWeapons = new List<RangeWeapon>();

            for (int i = 0; i < itemRecords.Count; i++)
            {
                ItemData itemData = gameStaticDataManager.GetGameData<ItemData>(itemRecords[i].itemID);
                if (itemData.ItemType != "Weapon")
                {
                    continue;
                }

                Weapon weapon = Instantiate(Resources.Load<Weapon>(itemData.PrefabPath), weaponSwitcherCache.transform);
                weaponSwitcherCache.AddWeapon(weapon);

                RangeWeapon rangeWeapon = weapon as RangeWeapon;
                if (rangeWeapon != null)
                {
                    rangeWeapons.Add(rangeWeapon);
                }
            }

            for (int i = 0; i < itemRecords.Count; i++)
            {
                ItemData itemData = gameStaticDataManager.GetGameData<ItemData>(itemRecords[i].itemID);

                if (itemData.ItemType != "Ammo")
                {
                    continue;
                }

                RangeWeapon rangeWeapon = weaponSwitcherCache.GetWeapon(itemData.Name) as RangeWeapon;
                if (rangeWeapon != null)
                {
                    rangeWeapon.SetRemainingAmmo(itemRecords[i].itemID, itemRecords[i].currentAmount);
                    rangeWeapons.Remove(rangeWeapon);
                }
            }

            for (int i = 0; i < rangeWeapons.Count; i++)
            {
                rangeWeapons[i].SetRemainingAmmo(-1, 0);
            }

            yield return null;

            weaponSwitcherCache.Initialize();
        }

        public void StartGame()
        {
            if (CurrentState != State.Ready)
            {
                Debug.LogError("LevelManager is not ready.");
                return;
            }

            CurrentState = State.Started;

            Audio.AudioManager.Instance.EnableWhiteNoise(startRoom.EnableWhiteNoise);
            Audio.AudioManager.Instance.PlayBGM(startRoom.BackgroundMusic);

            if (hero != null)
            {
                hero.PauseMove(false);
            }
        }

        private void EndGame(bool isWin)
        {
            EventBus.Publish(new Game_GameEnd() { isWin = isWin });
            CurrentState = State.None;
        }

        private void Update()
        {
            if (CurrentState == State.Started)
            {
                TickEndGame();

                for (int i = 0; i < actorTickers.Count; i++)
                {
                    if (pauseLocks.Count <= 0)
                    {
                        actorTickers[i].Tick();
                    }
                }

                // test 
                if (Input.GetKeyDown(KeyCode.F5))
                {
                    EventBus.Publish(new Game_TriggerSequence() { sequenceName = "GameFlow/NewGameFlow/_NewGameFlow" });
                }
            }
        }

        private void TickEndGame()
        {
            switch (endGameType)
            {
                case EndGameType.NeverEnd:
                    return;
                case EndGameType.AllEnemiesDead:
                    CheckAllEnemiesDead();
                    break;
            }
        }

        private void CheckAllEnemiesDead()
        {
            List<Actor> monsters = ActorContainer.GetActorsByCamp(Actor.Camp.Monster);

            if (hero.currentHealth <= 0)
            {
                StartCoroutine(IEEndGame(false));
                return;
            }

            bool isMonsterAllDead = true;
            for (int i = 0; i < monsters.Count; i++)
            {
                if (monsters[i].currentHealth > 0)
                {
                    isMonsterAllDead = false;
                    break;
                }
            }

            if (isMonsterAllDead)
            {
                StartCoroutine(IEEndGame(true));
            }
        }

        private System.Collections.IEnumerator IEEndGame(bool isWin)
        {
            CurrentState = State.None;
            actorController.SetControlTarget(null);
            yield return new WaitForSeconds(1f);
            EndGame(isWin);
        }
    }
}