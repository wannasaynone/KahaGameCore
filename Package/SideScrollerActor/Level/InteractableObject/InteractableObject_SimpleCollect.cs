using KahaGameCore.GameEvent;
using KahaGameCore.Package.SideScrollerActor.InGameEvent;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Level.InteractableObject
{
    public class InteractableObject_SimpleCollect : InteractableObject
    {
        [SerializeField] private int itemId = 0;
        [SerializeField] private int itemCount = 1;
        [SerializeField] private bool destroyAfterInteracted = true;
        [SerializeField] private int destoryIfItemAmountReached = 99;
        [SerializeField] private AudioClip collectSound;

        private void OnEnable()
        {
            if (itemId == 0)
            {
                Debug.LogError("Item ID is not set for " + name);
            }
        }

        private void Update()
        {
            if (LevelManager.GetItemCount(itemId) >= destoryIfItemAmountReached)
            {
                Destroy(gameObject);
            }
        }

        protected override void Interact()
        {
            EventBus.Publish(new InGameItem_OnAmountChanged()
            {
                itemID = itemId,
                addAmount = itemCount
            });

            if (destroyAfterInteracted)
            {
                Destroy(gameObject);
            }

            if (collectSound != null)
            {
                Audio.AudioManager.Instance.PlaySound(collectSound);
            }
        }

        protected override void Exit()
        {
        }
    }
}