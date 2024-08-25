using System;
using KahaGameCore.Package.DialogueSystem.DialogueCommand;
using KahaGameCore.Package.GameFlowSystem;
using UnityEngine;

public class GameStartMenu : MonoBehaviour
{
    [SerializeField] private TextAsset interactData;
    [SerializeField] private TextAsset dialogueData;
    [SerializeField] private GameObject deleteSaveButton;

    private void Start()
    {
        deleteSaveButton.SetActive(KahaGameCore.GameData.Implemented.JsonSaveDataHandler.IsSaveExist<KahaGameCore.Actor.GeneralValueContainer.SavableObject>(0));
    }

    public void StartGame()
    {
        DialogueCommandFactory dialogueCommandFactory = new DialogueCommandFactory(true);
        dialogueCommandFactory.RegisterCommandType("AddValue", typeof(DialogueCommand_AddValue));
        dialogueCommandFactory.RegisterCommandType("SetValue", typeof(DialogueCommand_SetValue));
        dialogueCommandFactory.RegisterCommandType("IfValue", typeof(DialogueCommand_IfValue));

        GameManager.Initialize(new InitializeFlowBase[]
        {
            new InitializeFlow_ReadData(dialogueData, interactData),
            new InitializeFlow_RegisterUserInterface()
        }, OnInitialCompleted, dialogueCommandFactory);

        gameObject.SetActive(false);
    }

    public void DeleteSave()
    {
        KahaGameCore.GameData.Implemented.JsonSaveDataHandler jsonSaveDataHandler = new KahaGameCore.GameData.Implemented.JsonSaveDataHandler(null, null);
        jsonSaveDataHandler.DeleteSave<KahaGameCore.Actor.GeneralValueContainer.SavableObject>(0);
        Start();
    }

    private void OnInitialCompleted()
    {
        GameManager.Insatance.LoadSave(0);
        if (SharedRepoditory.playerInstance.Stats.GetTotal("Day", true) == 0)
        {
            SharedRepoditory.playerInstance.Stats.SetBase("Day", 1);
            SharedRepoditory.playerInstance.Stats.SetBase("Time", 21);
            GameManager.Insatance.Save(0);
            new GameFlow_StartGame().Start();
        }
        else
        {
            new GameFlow_SelectAction().Start();
        }
    }
}