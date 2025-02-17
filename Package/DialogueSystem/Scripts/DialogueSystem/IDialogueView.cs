using System;
using UnityEngine;

namespace KahaGameCore.Package.DialogueSystem
{
    public interface IDialogueView
    {
        void SetLeftCharacterImage(Sprite sprite, Action onCompleted = null);
        void SetLeftCharacterImage(string spriteName, Action onCompleted = null);
        void SetRightCharacterImage(Sprite sprite, Action onCompleted = null);
        void SetRightCharacterImage(string spriteName, Action onCompleted = null);
        void HighlightLeftCharacterImage(Action onCompleted = null);
        void HighlightRightCharacterImage(Action onCompleted = null);
        void HighlightAllCharacterImage(Action onCompleted = null);
        void DehighlightLeftCharacterImage(Action onCompleted = null);
        void DehighlightRightCharacterImage(Action onCompleted = null);
        void DehighlightAllCharacterImage(Action onCompleted = null);
        void SetContentText(string text, Action onCompleted = null);
        void SetNameText(string text);
        IDialogueOptionButton AddOptionButton();
        bool IsWaitingSelection();
        void ShowCGImage(Sprite sprite, Action onCompleted = null);
        void ShowCGImage(string spriteName, Action onCompleted = null);
        void HideCGImage(Action onCompleted = null);
        void ClearOptions();
        void Hide(Action onCompleted = null);
    }
}