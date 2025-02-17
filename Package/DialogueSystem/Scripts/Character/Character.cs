using UnityEngine;

namespace KahaGameCore.Package.DialogueSystem
{
    public class Character : MonoBehaviour
    {
        [SerializeField] private int id;
        [SerializeField] private Transform targetTransform;
        [SerializeField] private bool cantTriggerDialogueIfRead;
        [SerializeField] private bool autoTriggerDialogue;
        [SerializeField] private bool flipable = true;
        [SerializeField] private bool DialogueRoot = false;


        public int ID
        {
            get
            {
                if (cantTriggerDialogueIfRead && PlayerManager.Instance.Player.HasReadDialogue(id))
                {
                    return -1;
                }

                return id;
            }
        }

        public Transform TargetTransform
        {
            get
            {
                if (cantTriggerDialogueIfRead && PlayerManager.Instance.Player.HasReadDialogue(id))
                {
                    return null;
                }
                return targetTransform;
            }
        }

        public Rigidbody2D Rigidbody { get; private set; }
        public bool AutoTriggerDialogue => autoTriggerDialogue;

        private void OnEnable()
        {
            CharacterContainer.AddCharacter(this);
            if (DialogueRoot)
            {
                Rigidbody = transform.parent.parent.GetComponent<Rigidbody2D>();
            }
            else
            {
                Rigidbody = GetComponent<Rigidbody2D>();
            }
        }

        private void OnDisable()
        {
            CharacterContainer.RemoveCharacter(this);
        }

        public void FlipCharacter(bool isFacingRight)
        {
            if (!flipable) return;

            transform.localScale = new Vector3(isFacingRight ? 1 : -1, 1, 1);
        }
    }
}