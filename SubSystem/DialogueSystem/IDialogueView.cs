using UnityEngine;

namespace KahaGameCore.SubSystem.DialogueSystem
{
    public interface IDialogueView
    {
        void SetLeftCharacterImage(Sprite sprite);
        void SetLeftCharacterImage(string spriteName);
        void SetRightCharacterImage(Sprite sprite);
        void SetRightCharacterImage(string spriteName);
        void SetContentText(string text);
        void SetNameText(string text);
        IDialogueOptionButton AddOptionButton();
        bool IsWaitingSelection();
        void ClearOptions();
        void Show(System.Action onCompleted);
        void Hide();
    }
}