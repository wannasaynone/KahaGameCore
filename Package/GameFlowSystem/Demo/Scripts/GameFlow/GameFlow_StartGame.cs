using System;
using KahaGameCore.Package.DialogueSystem;
using KahaGameCore.Package.GameFlowSystem;

public class GameFlow_StartGame : GameFlowBase
{
    public override void FixedUpdate()
    {
        throw new NotImplementedException();
    }

    public override void LateUpdate()
    {
        throw new NotImplementedException();
    }

    public override void Start()
    {
        DialogueManager.Instance.TriggerDialogue(0, SharedRepoditory.Find<DialogueView>(), OnDialogueEnd);
    }

    public override void Update()
    {
        throw new NotImplementedException();
    }

    private void OnDialogueEnd()
    {
        new GameFlow_SelectAction().Start();
    }
}
