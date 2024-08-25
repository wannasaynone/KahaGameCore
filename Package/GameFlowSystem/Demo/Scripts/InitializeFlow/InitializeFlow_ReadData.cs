using KahaGameCore.Package.GameFlowSystem;
using UnityEngine;

public class InitializeFlow_ReadData : InitializeFlowBase
{
    private readonly TextAsset dialogueData;
    private readonly TextAsset interactData;

    public InitializeFlow_ReadData(TextAsset dialogueData, TextAsset interactData)
    {
        this.dialogueData = dialogueData;
        this.interactData = interactData;
    }

    public override void Process(System.Action onComplete, System.Action onForceQuit)
    {
        KahaGameCore.GameData.Implemented.GameStaticDataManager gameStaticDataManager = new KahaGameCore.GameData.Implemented.GameStaticDataManager();
        KahaGameCore.GameData.Implemented.GameStaticDataDeserializer gameStaticDataDeserializer = new KahaGameCore.GameData.Implemented.GameStaticDataDeserializer();
        gameStaticDataManager.Add<KahaGameCore.Package.PlayerControlable.InteractData>(gameStaticDataDeserializer.Read<KahaGameCore.Package.PlayerControlable.InteractData[]>(interactData.text));
        gameStaticDataManager.Add<KahaGameCore.Package.DialogueSystem.DialogueData>(gameStaticDataDeserializer.Read<KahaGameCore.Package.DialogueSystem.DialogueData[]>(dialogueData.text));
        SharedRepoditory.gameStaticDataManager = gameStaticDataManager;
        onComplete?.Invoke();
    }
}