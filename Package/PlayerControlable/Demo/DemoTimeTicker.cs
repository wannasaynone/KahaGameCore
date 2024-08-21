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

    private InteractManager interactManager;

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
        interactManager = new InteractManager(gameStaticDataManager.GetAllGameData<InteractData>());

        KahaGameCore.Input.InputEventHanlder.UserInterface.OnMoveToNextOptionInView += OnMoveToNextOptionInView;
        KahaGameCore.Input.InputEventHanlder.UserInterface.OnMoveToPreviousOptionInView += OnMoveToPreviousOptionInView;
        KahaGameCore.Input.InputEventHanlder.UserInterface.OnOptionInViewSelected += OnOptionInViewSelected;

        generalActor = new GeneralActor();
    }

    private void OnOptionInViewSelected()
    {
        if (interactState != InteractState.SelectingAction)
        {
            return;
        }

        string returnString = interactManager.Interact(interactingObject.InteractTargetTag, actionTypes[selectingActionIndex], generalActor, day, time);
        Debug.Log("returnString=" + returnString);

        if (returnString == "Get A")
        {
            generalActor.Stats.Add("A", 1);
        }

        KahaGameCore.Input.InputEventHanlder.UnlockMovement(this);

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
        KahaGameCore.Input.InputEventHanlder.LockMovement(this);

        actionTypes = interactManager.GetAllActionType(interactableObject.InteractTargetTag);
        for (int i = 0; i < actionTypes.Length; i++)
        {
            Debug.Log(actionTypes[i]);
        }
        Debug.Log("Current selecting action: " + actionTypes[selectingActionIndex]);
    }
}
