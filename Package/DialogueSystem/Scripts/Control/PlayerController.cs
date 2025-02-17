using System.Collections;
using UnityEngine;

namespace KahaGameCore.Package.DialogueSystem
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float speed = 5f;
        [SerializeField] private float maxInteractDistance = 1.5f;
        [SerializeField] private Character controlledCharacter;
        [SerializeField] private CameraController mainCamera;
        [SerializeField] private AudioClip interactSound;

        private enum MoveDirection
        {
            Up,
            Down,
            Left,
            Right,
            None
        }

        private MoveDirection moveDirection = MoveDirection.None;
        private Character lastAutoTriggeredCharacter;

        private void Update()
        {
            if (InputDetector.IsMovementLocked())
            {
                moveDirection = MoveDirection.None;
                return;
            }

            Character closetCharacter = CharacterContainer.GetClosetCharacter(controlledCharacter, maxInteractDistance);
            if (closetCharacter != null && closetCharacter.ID > 0)
            {
                if ((closetCharacter != lastAutoTriggeredCharacter) || (Vector2.Distance(controlledCharacter.transform.position, closetCharacter.transform.position) > maxInteractDistance))
                    lastAutoTriggeredCharacter = null;

                if (InputDetector.IsInteracting() || (closetCharacter.AutoTriggerDialogue && lastAutoTriggeredCharacter != closetCharacter && Vector2.Distance(controlledCharacter.transform.position, closetCharacter.transform.position) <= maxInteractDistance / 2f))
                {
                    moveDirection = MoveDirection.None;

                    SoundManager.Instance.PlaySFX(interactSound);

                    if (closetCharacter.transform.position.x > controlledCharacter.transform.position.x)
                        closetCharacter.FlipCharacter(true);
                    else
                        closetCharacter.FlipCharacter(false);

                    if (closetCharacter.AutoTriggerDialogue)
                        lastAutoTriggeredCharacter = closetCharacter;


                    if (closetCharacter.TargetTransform != null)
                    {
                        StartCoroutine(TriggerDialogueManager(closetCharacter));

                    }
                    else
                    {
                        DialogueManager.Instance.TriggerDialogue(closetCharacter.ID, UserInterfaceManager.Instance.DialogueView);
                    }

                    return;
                }
            }
            if (InputDetector.IsMovingUp())
                moveDirection = MoveDirection.Up;
            else if (InputDetector.IsMovingDown())
                moveDirection = MoveDirection.Down;
            else if (InputDetector.IsMovingLeft())
                moveDirection = MoveDirection.Left;
            else if (InputDetector.IsMovingRight())
                moveDirection = MoveDirection.Right;
            else
                moveDirection = MoveDirection.None;
        }

        private void FixedUpdate()
        {
            if (moveDirection == MoveDirection.Up)
                controlledCharacter.Rigidbody.MovePosition(controlledCharacter.Rigidbody.transform.position + speed * Time.deltaTime * Vector3.up);

            if (moveDirection == MoveDirection.Down)
                controlledCharacter.Rigidbody.MovePosition(controlledCharacter.Rigidbody.transform.position + speed * Time.deltaTime * Vector3.down);

            if (moveDirection == MoveDirection.Left)
                controlledCharacter.Rigidbody.MovePosition(controlledCharacter.Rigidbody.transform.position + speed * Time.deltaTime * Vector3.left);

            if (moveDirection == MoveDirection.Right)
                controlledCharacter.Rigidbody.MovePosition(controlledCharacter.Rigidbody.transform.position + speed * Time.deltaTime * Vector3.right);
        }
        IEnumerator TriggerDialogueManager(Character closetCharacter)
        {
            InputDetector.LockMovement(this);
            yield return new WaitForSeconds(0.5f);
            GeneralBlackScreen.Instance.FadeIn(() =>
            {
                mainCamera.ChangeTrget(closetCharacter.TargetTransform);
                GeneralBlackScreen.Instance.FadeOut(() =>
                {
                    InputDetector.UnlockMovement(this);
                    DialogueManager.Instance.TriggerDialogue(closetCharacter.ID, UserInterfaceManager.Instance.DialogueView);
                });
            });

        }
    }
}