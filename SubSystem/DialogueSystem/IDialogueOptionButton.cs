namespace KahaGameCore.DialogueSystem
{
    public interface IDialogueOptionButton
    {
        void SetUpButtonText(string text);
        void SetUpOnClicked(System.Action action);
        void OnClicked();
    }
}