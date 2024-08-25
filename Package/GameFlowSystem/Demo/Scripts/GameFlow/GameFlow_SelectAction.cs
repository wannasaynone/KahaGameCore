using KahaGameCore.Package.DialogueSystem;
using KahaGameCore.Package.GameFlowSystem;
using KahaGameCore.Package.PlayerControlable;

public class GameFlow_SelectAction : GameFlowBase
{
    private InGameMenu inGameMenu;

    private string[] currentInteractResult;
    private int currentProcessIndex;

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
            currentInteractResult = returnValueString.Split('|');
            currentProcessIndex = 0;
            ProcessInteractResult();
        }
        else
        {
            UnityEngine.Debug.LogError("action: " + action + " does not have any return value. day: " + SharedRepoditory.playerInstance.Stats.GetTotal("Day", true) + " time: " + SharedRepoditory.playerInstance.Stats.GetTotal("Time", true));
        }
    }

    private void ProcessInteractResult()
    {
        if (currentProcessIndex < currentInteractResult.Length)
        {
            string[] process = currentInteractResult[currentProcessIndex].Split(':');
            string processType = process[0];
            string processValue = process.Length > 1 ? process[1] : "";

            switch (processType)
            {
                case "Trigger":
                    DialogueManager.Instance.TriggerDialogue(int.Parse(processValue), SharedRepoditory.Find<DialogueView>(), OnDialogueEnd);
                    return;
                case "GoNextDay":
                    SharedRepoditory.playerInstance.Stats.AddBase("Day", 1);
                    SharedRepoditory.playerInstance.Stats.SetBase("Time", 8);
                    break;
                case "Time":
                    SharedRepoditory.playerInstance.Stats.SetBase("Time", int.Parse(processValue));
                    break;
                case "AddTime":
                    SharedRepoditory.playerInstance.Stats.AddBase("Time", int.Parse(processValue));
                    break;
                default:
                    UnityEngine.Debug.LogError("Unknown process type: " + processType);
                    return;
            }

            currentProcessIndex++;
            ProcessInteractResult();
        }
        else
        {
            if (SharedRepoditory.playerInstance.Stats.GetTotal("Time", true) >= 10 && SharedRepoditory.playerInstance.Stats.GetTotal("Time", true) <= 11)
            {
                SharedRepoditory.playerInstance.Stats.SetBase("DidWork", 0);
            }

            GameManager.Insatance.Save(0);
            inGameMenu.gameObject.SetActive(true);
        }
    }

    private void OnDialogueEnd()
    {
        currentProcessIndex++;
        ProcessInteractResult();
    }

    public override void Update()
    {
        throw new System.NotImplementedException();
    }
}