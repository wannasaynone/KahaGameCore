using KahaGameCore.Package.SideScrollerActor.Camera;
using KahaGameCore.Package.SideScrollerActor.InGameEvent;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Level.InteractableObject
{
    public class Portal : InteractableObject, IInteractableObject
    {
        [SerializeField] private AudioClip enterSound;
        [SerializeField] private Animator animator;
        [SerializeField] private bool autoTeleport = false;
        [SerializeField] private string targetRoomName = "";

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
            if (autoTeleport)
            {
                AutoTeleport();
            }
            else
            {
                SetWaitingInteractObject();
            }
        }

        private void AutoTeleport()
        {
            RoomSetting targetRoom = LevelManager.GetRoomSettingByName(targetRoomName);

            if (targetRoom == null)
            {
                Debug.LogError("targetRoom cannot be null for portal: " + gameObject.name);
                return;
            }

            interactingPortal = new Game_OnPortalEntered
            {
                actorInstanceID = actor.GetInstanceID(),
                targetPosition = targetRoom.GetSpawnPointByFromRoomName(transform.parent.name).transform.position,
                board_min = targetRoom.BoardTransform_min.position.x,
                board_max = targetRoom.BoardTransform_max.position.x,
                isBackPortal = false,
#if USING_URP
                volumeProfile = targetRoom.VolumeProfile,
#endif
                enableWhiteNoise = targetRoom.EnableWhiteNoise,
                enterSound = enterSound
            };

            Enter();
        }

        private void SetWaitingInteractObject()
        {
            RoomSetting targetRoom = LevelManager.GetRoomSettingByName(targetRoomName);

            if (targetRoom == null)
            {
                Debug.LogError("targetRoom cannot be null for portal: " + gameObject.name);
                return;
            }

            interactingPortal = new Game_OnPortalEntered
            {
                actorInstanceID = actor.GetInstanceID(),
                targetPosition = targetRoom.GetSpawnPointByFromRoomName(transform.parent.name).transform.position,
                board_min = targetRoom.BoardTransform_min.position.x,
                board_max = targetRoom.BoardTransform_max.position.x,
                isBackPortal = false,
#if USING_URP
                volumeProfile = targetRoom.VolumeProfile,
#endif
                enableWhiteNoise = targetRoom.EnableWhiteNoise,
                enterSound = enterSound
            };

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
#if USING_URP
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