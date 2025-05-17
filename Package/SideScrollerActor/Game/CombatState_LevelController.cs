using System.Collections;
using System.Collections.Generic;
using KahaGameCore.GameData.Implemented;
using KahaGameCore.GameEvent;
using KahaGameCore.Package.SideScrollerActor.Gameplay;
using KahaGameCore.Package.SideScrollerActor.InGameEvent;
using KahaGameCore.Package.SideScrollerActor.Level;
using KahaGameCore.Package.SideScrollerActor.View;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Game
{
    public class CombatState_LevelController
    {
        private LevelManager currentLevelManager;
        private readonly InGameView inGameView;
        private readonly List<OnItemRecordChanged> itemRecord;
        private readonly GameStaticDataManager gameStaticDataManager;

        private int cachedHeroHealth = -1;
        private float cachedHeroStamina = -1f;

        public CombatState_LevelController(InGameView inGameView, List<OnItemRecordChanged> itemRecord, GameStaticDataManager gameStaticDataManager)
        {
            this.itemRecord = itemRecord;
            this.gameStaticDataManager = gameStaticDataManager;
            this.inGameView = inGameView;
        }

        public void StartListen()
        {
            EventBus.Subscribe<Actor_OnWeaponChanged>(OnWeaponSwitched);
            EventBus.Subscribe<RangeWeapon_OnAmmoAmountChanged>(OnRangeWeaponAmmoAmountChanged);
            EventBus.Subscribe<Game_OnNextLevelRequested>(OnNextLevelRequested);
            EventBus.Subscribe<Actor_OnHealthChanged>(OnActorHealthChanged);
        }

        public void StopListen()
        {
            EventBus.Unsubscribe<Game_OnNextLevelRequested>(OnNextLevelRequested);
            EventBus.Unsubscribe<Actor_OnHealthChanged>(OnActorHealthChanged);
            EventBus.Unsubscribe<Actor_OnWeaponChanged>(OnWeaponSwitched);
            EventBus.Unsubscribe<RangeWeapon_OnAmmoAmountChanged>(OnRangeWeaponAmmoAmountChanged);
        }

        public void DestroyCurrentLevel()
        {
            if (currentLevelManager != null)
            {
                Object.Destroy(currentLevelManager.gameObject);
                currentLevelManager = null;
            }
            else
            {
                Debug.LogError("Current level manager is null, cannot destroy it. Did you call Game_OnNextLevelRequested?");
            }
        }

        public void StartLevel()
        {
            if (currentLevelManager != null)
            {
                currentLevelManager.StartGame();
            }
            else
            {
                Debug.LogError("Current level manager is null, cannot start the level. Did you call Game_OnNextLevelRequested?");
            }
        }

        private void OnNextLevelRequested(Game_OnNextLevelRequested e)
        {
            inGameView.gameObject.SetActive(false);
            Audio.AudioManager.Instance.StopBGM();

            KahaGameCore.Common.GeneralCoroutineRunner.Instance.StartCoroutine(IELoadNextLevel(e));
        }

        private IEnumerator IELoadNextLevel(Game_OnNextLevelRequested e)
        {
            LevelManager levelPrefab = Resources.Load<LevelManager>(e.nextLevelName);

            if (levelPrefab == null)
            {
                Debug.LogError("Level prefab not found: " + e.nextLevelName);
                yield break;
            }

            EventBus.Unsubscribe<Actor_OnHealthChanged>(OnActorHealthChanged);

            if (currentLevelManager != null)
            {
                Object.Destroy(currentLevelManager.gameObject);
                currentLevelManager = null;
            }

            yield return new WaitForSeconds(0.1f); // for waiting destory level and clear all actors

            cachedHeroHealth = e.heroHealth;
            cachedHeroStamina = e.heroStamina;

            currentLevelManager = Object.Instantiate(levelPrefab);

            inGameView.Initialize();

            // any other initialization??

            currentLevelManager.Initialize(itemRecord, gameStaticDataManager, cachedHeroHealth, cachedHeroStamina);
            yield return new WaitUntil(() => currentLevelManager.CurrentState == LevelManager.State.Ready);

            Actor hero = ActorContainer.GetActorByCamp(Actor.Camp.Hero);
            inGameView.SetHeroHealth((float)hero.currentHealth / hero.currentMaxHealth);
            inGameView.UpdateDelayImmediatly();

            cachedHeroHealth = -1;
            cachedHeroStamina = -1;

            EventBus.Subscribe<Actor_OnHealthChanged>(OnActorHealthChanged);

            EventBus.Publish(new Game_OnLevelLoaded { levelName = e.nextLevelName });
        }

        private void OnActorHealthChanged(Actor_OnHealthChanged e)
        {
            Actor actor = ActorContainer.GetActorByInstanceID(e.instanceID);

            if (actor == null)
            {
                return;
            }

            if (actor.camp == Actor.Camp.Hero)
            {
                inGameView.SetHeroHealth((float)actor.currentHealth / actor.currentMaxHealth);
            }
        }

        private void OnWeaponSwitched(Actor_OnWeaponChanged e)
        {
            Actor actor = ActorContainer.GetActorByInstanceID(e.actorInstanceID);
            if (actor.camp == Actor.Camp.Hero)
            {
                inGameView.SetWeaponState(actor.Weapon as WeaponScripts.Weapon);
            }
        }

        private void OnRangeWeaponAmmoAmountChanged(RangeWeapon_OnAmmoAmountChanged e)
        {
            inGameView.SetWeaponState(ActorContainer.GetActorByCamp(Actor.Camp.Hero).Weapon as WeaponScripts.Weapon);
        }
    }
}
