using KahaGameCore.Package.SideScrollerActor.Camera;
using KahaGameCore.Package.SideScrollerActor.InGameEvent;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Level.InteractableObject
{
    public class Portal : InteractableObject, IInteractableObject
    {
        [SerializeField] private AudioClip enterSound;
        [SerializeField] private Animator animator;
        [Header("如果沒有設定target room表示返回最後一次記憶的傳送門處")]
        [SerializeField] private RoomSetting targetRoom;

        protected override void Exit()
        {
            actor.SetWaitingInteractObject(null);

            if (animator != null)
            {
                animator.Play("None");
            }
        }

        protected override void Interact()
        {
            if (targetRoom != null)
            {
                backFromPortalInfo = new Game_OnPortalEntered
                {
                    actorInstanceID = GetInstanceID(),
                    targetPosition = new Vector3(transform.position.x, actor.transform.position.y, actor.transform.position.z),
                    board_min = BoardSetter.MIN_X,
                    board_max = BoardSetter.MAX_X,
                    isBackPortal = true,
                    enableWhiteNoise = Audio.AudioManager.Instance.IsPlayingWhiteNoise,
#if UNIVERSAL_PIPELINE_CORE_INCLUDED
                    volumeProfile = LevelManager.GetCurrentVolumeProfile()
#endif
                };

                interactingPortal = new Game_OnPortalEntered
                {
                    actorInstanceID = actor.GetInstanceID(),
                    targetPosition = targetRoom.SpawnPoint.position,
                    board_min = targetRoom.BoardTransform_min.position.x,
                    board_max = targetRoom.BoardTransform_max.position.x,
                    isBackPortal = false,
#if UNIVERSAL_PIPELINE_CORE_INCLUDED
                    volumeProfile = targetRoom.VolumeProfile,
#endif
                    enableWhiteNoise = targetRoom.EnableWhiteNoise,
                    enterSound = enterSound
                };
            }
            else
            {
                interactingPortal = backFromPortalInfo;
            }

            actor.SetWaitingInteractObject(this);

            if (animator != null)
            {
                animator.Play("PortalInteracting");
            }
        }

        void IInteractableObject.Interact()
        {
            Enter();
        }

        private static Game_OnPortalEntered backFromPortalInfo = null;
        private static Game_OnPortalEntered interactingPortal = null;

        private void Enter()
        {
            if (interactingPortal == null)
            {
                return;
            }

            LevelManager.Pause();
            if (interactingPortal.enterSound != null) Audio.AudioManager.Instance.PlaySound(interactingPortal.enterSound);
            Utlity.GeneralBlackScreen.Instance.FadeIn(OnPortalFadeInEnded);
        }

        private void OnPortalFadeInEnded()
        {
            BoardSetter.SetBoard(interactingPortal.board_min, interactingPortal.board_max);
#if UNIVERSAL_PIPELINE_CORE_INCLUDED
            if (interactingPortal.volumeProfile != null)
            {
                LevelManager.SetProfile(interactingPortal.volumeProfile);
            }
#endif
            actor.transform.position = interactingPortal.targetPosition;
            CameraController.Instance.SetToTargetPositionImmediately();

            interactingPortal = null;
            Utlity.GeneralBlackScreen.Instance.FadeOut(OnPortalFadeOutEnded);
        }

        private void OnPortalFadeOutEnded()
        {
            LevelManager.Resume();
        }
    }
}