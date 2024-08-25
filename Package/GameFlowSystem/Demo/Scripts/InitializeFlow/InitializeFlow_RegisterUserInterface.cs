using UnityEngine;

public class InitializeFlow_RegisterUserInterface : KahaGameCore.Package.GameFlowSystem.InitializeFlowBase
{
    public override void Process(System.Action onComplete, System.Action onForceQuit)
    {
        KahaGameCore.Package.GameFlowSystem.SharedRepoditory.AddSharedComponent(Object.FindObjectOfType<GameStartMenu>(true));
        KahaGameCore.Package.GameFlowSystem.SharedRepoditory.AddSharedComponent(Object.FindObjectOfType<InGameMenu>(true));
        KahaGameCore.Package.GameFlowSystem.SharedRepoditory.AddSharedComponent(Object.FindObjectOfType<KahaGameCore.Package.DialogueSystem.DialogueView>(true));
        onComplete?.Invoke();
    }
}