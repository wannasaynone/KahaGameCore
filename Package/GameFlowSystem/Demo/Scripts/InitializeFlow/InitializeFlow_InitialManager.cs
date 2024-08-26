using System;
using KahaGameCore.Package.DialogueSystem;
using KahaGameCore.Package.DialogueSystem.DialogueCommand;
using KahaGameCore.Package.GameFlowSystem;
using KahaGameCore.Package.PlayerControlable;

public class InitializeFlow_InitialManager : InitializeFlowBase
{
    public override void Process(Action onComplete, Action onForceQuit)
    {
        DialogueCommandFactory dialogueCommandFactory = new DialogueCommandFactory(true);
        dialogueCommandFactory.RegisterCommandType("AddValue", typeof(DialogueCommand_AddValue));
        dialogueCommandFactory.RegisterCommandType("SetValue", typeof(DialogueCommand_SetValue));
        dialogueCommandFactory.RegisterCommandType("IfValue", typeof(DialogueCommand_IfValue));

        InteractManager.Initialize(SharedRepoditory.gameStaticDataManager.GetAllGameData<InteractData>());
        DialogueManager.Initialize(SharedRepoditory.gameStaticDataManager.GetAllGameData<DialogueData>(), dialogueCommandFactory);

        onComplete?.Invoke();
    }
}
