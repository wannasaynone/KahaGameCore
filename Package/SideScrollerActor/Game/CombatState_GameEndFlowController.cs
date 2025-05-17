using System;
using System.Collections;
using KahaGameCore.GameEvent;
using KahaGameCore.Package.DialogueSystem;
using KahaGameCore.Package.SideScrollerActor.Camera;
using KahaGameCore.Package.SideScrollerActor.InGameEvent;
using KahaGameCore.Package.SideScrollerActor.View;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Game
{
    public class CombatState_GameEndFlowController
    {
        private GeneralAnimationPlayer cloneHCG;
        private readonly CombatState_LevelController levelController;
        private readonly InGameView inGameView;
        private readonly GameEndView gameEndView;
        private readonly Action onConfirmClicked;

        public CombatState_GameEndFlowController(CombatState_LevelController levelController, InGameView inGameView, GameEndView gameEndView, Action onConfirmClicked)
        {
            this.levelController = levelController;
            this.inGameView = inGameView;
            this.gameEndView = gameEndView;
            this.onConfirmClicked = onConfirmClicked;
        }

        public void StartListen()
        {
            EventBus.Subscribe<GameEndView_OnConfirmClicked>(OnGameEndViewConfirmClicked);
            EventBus.Subscribe<Game_GameEnd>(OnGameEnded);
        }

        public void StopListen()
        {
            EventBus.Unsubscribe<GameEndView_OnConfirmClicked>(OnGameEndViewConfirmClicked);
            EventBus.Unsubscribe<Game_GameEnd>(OnGameEnded);
        }

        private void OnGameEnded(Game_GameEnd e)
        {
            inGameView.gameObject.SetActive(false);
            Audio.AudioManager.Instance.StopBGM();

            if (e.isWin)
            {
                gameEndView.gameObject.SetActive(true);
                gameEndView.ShowWinPanel();
            }
            else
            {
                Common.GeneralCoroutineRunner.Instance.StartCoroutine(IEStartLoseCG());
            }
        }

        private IEnumerator IEStartLoseCG()
        {
            bool fadeInEnded = false;
            Utlity.GeneralBlackScreen.Instance.FadeIn(delegate
            {
                levelController.DestroyCurrentLevel();
                fadeInEnded = true;
            });

            yield return new WaitUntil(() => fadeInEnded);

            CameraController.Instance.offset = new Vector3(0, 0, 0);
            CameraController.Instance.target = null;
            CameraController.Instance.ResetOrthographicSizeImmediately();
            CameraController.Instance.transform.position = new Vector3(0, 0, 0);

            // TODO: get cg name from died enemy
            cloneHCG = UnityEngine.Object.Instantiate(Resources.Load<GeneralAnimationPlayer>("HCG/HCG-001"), gameEndView.LoseCGRoot);
            cloneHCG.transform.localPosition = new Vector3(0, 0, 0);
            cloneHCG.transform.localScale = new Vector3(1, 1, 1);
            cloneHCG.transform.localRotation = Quaternion.Euler(0, 0, 0);

            gameEndView.gameObject.SetActive(true);
            gameEndView.ShowLosePanel();

            bool isFadeOutEnded = false;
            Utlity.GeneralBlackScreen.Instance.FadeOut(delegate
            {
                isFadeOutEnded = true;
            });

            yield return new WaitUntil(() => isFadeOutEnded);
        }

        private void OnGameEndViewConfirmClicked(GameEndView_OnConfirmClicked e)
        {
            Utlity.GeneralBlackScreen.Instance.FadeIn(delegate
            {
                if (cloneHCG != null)
                {
                    UnityEngine.Object.Destroy(cloneHCG.gameObject);
                    cloneHCG = null;
                }
                gameEndView.gameObject.SetActive(false);
                onConfirmClicked?.Invoke();
            });
        }
    }
}