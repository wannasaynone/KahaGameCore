using UnityEngine;
using KahaGameCore.Package.SideScrollerActor.WeaponScripts;
using KahaGameCore.Package.SideScrollerActor.Camera;

namespace KahaGameCore.Package.SideScrollerActor.Gameplay
{
    public partial class Actor
    {
        public void ForceUpdateCameraSettingWithThisActor()
        {
            if (state == State.Flying)
            {
                CameraController.Instance.offset = IsFacingRight ? new Vector3(cameraOffset_normal, 0f, 0f) : new Vector3(-cameraOffset_normal, 0f, 0f);
                CameraController.Instance.lerpSpeed = 0.02f;
                return;
            }

            IAttackInfo nextAttackInfo = null;
            if (weaponCapability != null)
            {
                nextAttackInfo = weaponCapability.PeekNextAttackInfo();
            }

            if (isPreparingAttack)
            {
                if (nextAttackInfo == null)
                {
                    CameraController.Instance.offset = IsFacingRight ? new Vector3(cameraOffset_normal, cameraOffset_y, 0f) : new Vector3(-cameraOffset_normal, cameraOffset_y, 0f);
                }
                else
                {
                    CameraController.Instance.offset = IsFacingRight ? new Vector3(nextAttackInfo.GetCameraOffsetPrepareAttack(), cameraOffset_y, 0f) : new Vector3(-nextAttackInfo.GetCameraOffsetPrepareAttack(), cameraOffset_y, 0f);
                }
            }
            else
            {
                CameraController.Instance.offset = IsFacingRight ? new Vector3(cameraOffset_normal, cameraOffset_y, 0f) : new Vector3(-cameraOffset_normal, cameraOffset_y, 0f);
            }

            float cameraToTagrgetPositionDistance = Mathf.Abs(CameraController.Instance.transform.position.x - (CameraController.Instance.target.position.x + CameraController.Instance.offset.x));
            float targetPositionToActorPositionDistance = Mathf.Abs(CameraController.Instance.target.position.x + CameraController.Instance.offset.x - transform.position.x);

            if (isPreparingAttack)
            {
                if (CameraController.Instance.lerpSpeed > 0.06f) // means was in idle/walk (0.08f or 1f)
                {
                    CameraController.Instance.lerpSpeed = 0.06f;
                }
                else // means is returning form looking back speed
                {
                    CameraController.Instance.lerpSpeed = Mathf.Lerp(CameraController.Instance.lerpSpeed, 0.06f, 0.1f);
                }

                // default is 0.06f

                Actor cloestActor = ActorContainer.GetCloestOpponent(this);
                if (cameraToTagrgetPositionDistance > targetPositionToActorPositionDistance) // means looking back
                {
                    if ((isPressingLeft && !IsFacingRight) || (isPressingRight && IsFacingRight)) // means is aiming and moving
                    {
                        CameraController.Instance.lerpSpeed = 0.1f;
                    }
                    else
                    {
                        CameraController.Instance.lerpSpeed = 0.02f;
                    }
                }
                else if (cloestActor != null && Mathf.Abs(cloestActor.transform.position.x - transform.position.x) <= 2f) // means is aiming at close enemy
                {
                    CameraController.Instance.lerpSpeed = 0.02f;
                }
            }
            else
            {
                if (cameraToTagrgetPositionDistance >= 0.02f) // means it is lerping back
                {
                    if (state == State.Running
                        || (isPressingLeft && CameraController.Instance.transform.position.x > transform.position.x)
                        || (isPressingRight && CameraController.Instance.transform.position.x < transform.position.x)) // means camera is chasing actor from back
                    {
                        CameraController.Instance.lerpSpeed = 0.12f;
                    }
                    else
                    {
                        CameraController.Instance.lerpSpeed = 0.02f;
                    }
                }
                else // means camera is in position
                {
                    CameraController.Instance.lerpSpeed = 1f;
                }
            }
        }
    }
}
