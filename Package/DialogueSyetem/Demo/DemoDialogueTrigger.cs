using System;
using KahaGameCore.DialogueSystem;
using UnityEngine;

public class DemoDialogueTrigger : MonoBehaviour
{
    [SerializeField] private TextAsset dialogueDataSource;
    [SerializeField] private DialogueView dialogueView;
    [SerializeField] private GameObject demoButtonRoot;

    private void Start()
    {
        KahaGameCore.GameData.Implemented.GameStaticDataManager gameStaticDataManager = new KahaGameCore.GameData.Implemented.GameStaticDataManager();
        KahaGameCore.GameData.Implemented.GameStaticDataDeserializer gameStaticDataDeserializer = new KahaGameCore.GameData.Implemented.GameStaticDataDeserializer();

        gameStaticDataManager.Add<DialogueData>(gameStaticDataDeserializer.Read<DialogueData[]>(dialogueDataSource.text));

        KahaGameCore.DialogueSystem.DialogueCommand.DialogueCommandFactory dialogueCommandFactory = new KahaGameCore.DialogueSystem.DialogueCommand.DialogueCommandFactory(true);

        DialogueManager.Initialize(gameStaticDataManager.GetAllGameData<DialogueData>(), dialogueCommandFactory);
    }

    public void TriggerDialogue(int id)
    {
        demoButtonRoot.SetActive(false);
        DialogueManager.Instance.TriggerDialogue(id, dialogueView, OnCompleted);
    }

    private void OnCompleted()
    {
        demoButtonRoot.SetActive(true);
    }
}
