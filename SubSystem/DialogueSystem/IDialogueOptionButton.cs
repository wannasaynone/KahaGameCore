namespace KahaGameCore.SubSystem.DialogueSystem
{
    public interface IDialogueOptionButton
    {
        void SetUpButtonText(string text);
        void SetUpOnClicked(System.Action action);
        void OnClicked();
    }
}