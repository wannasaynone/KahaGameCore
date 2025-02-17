using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KahaGameCore.Package.DialogueSystem
{
    public class DialogueView_OptionButton : MonoBehaviour, IDialogueOptionButton, IPointerEnterHandler
    {
        public Action<DialogueView_OptionButton> OnHovered;

        [SerializeField] private TMPro.TextMeshProUGUI buttonText;

        private Action onClicked;

        public void SetUpButtonText(string text)
        {
            buttonText.text = text;
        }

        public void SetUpOnClicked(System.Action action)
        {
            onClicked = action;
        }

        public void OnClicked()
        {
            onClicked?.Invoke();
        }

        public void SetSelect(bool active)
        {
            transform.localScale = active ? Vector3.one * 1.1f : Vector3.one;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            OnHovered?.Invoke(this);
        }
    }
}