using KahaGameCore.Actor;
using KahaGameCore.Package.PlayerControlable;
using UnityEngine;

public class DemoTimeTicker : MonoBehaviour
{
    public int day;
    public int time;

    private void LateUpdate()
    {
        if (interactState == InteractState.WaitOneFrame)
        {
            interactState = InteractState.Idle;
        }
    }

    [SerializeField] private TextAsset interactDataTextAsset;

    private enum InteractState
    {
        Idle,
        SelectingAction,
        WaitOneFrame
    }

    private InteractState interactState = InteractState.Idle;

    private string[] actionTypes;
    private int selectingActionIndex = 0;
    private InteractableObject interactingObject;

    private GeneralActor generalActor;

    private void Awake()
    {
        KahaGameCore.GameData.Implemented.GameStaticDataManager gameStaticDataManager = new KahaGameCore.GameData.Implemented.GameStaticDataManager();
        KahaGameCore.GameData.Implemented.GameStaticDataDeserializer gameStaticDataDeserializer = new KahaGameCore.GameData.Implemented.GameStaticDataDeserializer();
        gameStaticDataManager.Add<InteractData>(gameStaticDataDeserializer.Read<InteractData[]>(interactDataTextAsset.text));
        InteractManager.Initialize(gameStaticDataManager.GetAllGameData<InteractData>());

        generalActor = new GeneralActor();
    }

    private void OnOptionInViewSelected()
    {
        if (interactState != InteractState.SelectingAction)
        {
            return;
        }

        string returnString = InteractManager.Instance.Interact(interactingObject.InteractTargetTag, actionTypes[selectingActionIndex], generalActor, day, time);
        Debug.Log("returnString=" + returnString);

        if (returnString == "Get A")
        {
            generalActor.Stats.Add("A", 1);
        }

        interactState = InteractState.WaitOneFrame;
    }

    private void OnMoveToPreviousOptionInView()
    {
        if (interactState != InteractState.SelectingAction)
        {
            return;
        }

        selectingActionIndex -= 1;
        if (selectingActionIndex < 0)
        {
            selectingActionIndex = actionTypes.Length - 1;
        }
        Debug.Log("Current selecting action: " + actionTypes[selectingActionIndex]);
    }

    private void OnMoveToNextOptionInView()
    {
        if (interactState != InteractState.SelectingAction)
        {
            return;
        }

        selectingActionIndex += 1;
        if (selectingActionIndex >= actionTypes.Length)
        {
            selectingActionIndex = 0;
        }
        Debug.Log("Current selecting action: " + actionTypes[selectingActionIndex]);
    }

    public void OnInteractCalled(InteractableObject interactableObject)
    {
        if (interactState != InteractState.Idle)
        {
            return;
        }

        interactingObject = interactableObject;

        interactState = InteractState.SelectingAction;

        actionTypes = InteractManager.Instance.GetAllActionType(interactableObject.InteractTargetTag, generalActor, day, time);
        Debug.Log("getting actionTypes for " + interactableObject.InteractTargetTag);
        for (int i = 0; i < actionTypes.Length; i++)
        {
            Debug.Log("available option: " + actionTypes[i]);
        }
        Debug.Log("Current selecting action: " + actionTypes[selectingActionIndex]);
    }
}
