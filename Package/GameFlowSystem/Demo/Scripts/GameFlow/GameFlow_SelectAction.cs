using KahaGameCore.Package.DialogueSystem;
using KahaGameCore.Package.GameFlowSystem;
using KahaGameCore.Package.PlayerControlable;

public class GameFlow_SelectAction : GameFlowBase
{
    private InGameMenu inGameMenu;

    public override void FixedUpdate()
    {
        throw new System.NotImplementedException();
    }

    public override void LateUpdate()
    {
        throw new System.NotImplementedException();
    }

    public override void Start()
    {
        inGameMenu = SharedRepoditory.Find<InGameMenu>();
        inGameMenu.OnActionSelected += OnActionSelected;
        inGameMenu.gameObject.SetActive(true);
    }

    private void OnActionSelected(string action)
    {
        string returnValueString = InteractManager.Instance.Interact("InGameMenu",
                                                                    action,
                                                                    SharedRepoditory.playerInstance,
                                                                    SharedRepoditory.playerInstance.Stats.GetTotal("Day", true),
                                                                    SharedRepoditory.playerInstance.Stats.GetTotal("Time", true));

        if (!string.IsNullOrEmpty(returnValueString))
        {
            inGameMenu.gameObject.SetActive(false);
            int dialogueID = int.Parse(returnValueString);
            DialogueManager.Instance.TriggerDialogue(dialogueID, SharedRepoditory.Find<DialogueView>(), OnDialogueEnd);
        }
    }

    private void OnDialogueEnd()
    {
        inGameMenu.gameObject.SetActive(true);
    }

    public override void Update()
    {
        throw new System.NotImplementedException();
    }
}