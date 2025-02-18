using KahaGameCore.GameData.Implemented;
using UnityEngine;

namespace KahaGameCore.Package.DialogueSystem.Example
{
    public class DialogueSystemExample : MonoBehaviour
    {
        [SerializeField] private TextAsset dialogueDataTextAsset;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private DialogueView dialogueView;

        private void Start()
        {
            PlayerManager.Initialize();

            GameStaticDataManager gameStaticDataManager = new GameStaticDataManager();
            GameStaticDataDeserializer gameStaticDataDeserializer = new GameStaticDataDeserializer();
            gameStaticDataManager.Add<DialogueData>(gameStaticDataDeserializer.Read<DialogueData[]>(dialogueDataTextAsset.text));

            DialogueManager.Initialize(gameStaticDataManager, new DialogueCommandFactory(cameraController, PlayerManager.Instance.Player));
        }

        private void Update()
        {
            if (Input.GetKeyUp(KeyCode.Alpha1))
            {
                DialogueManager.Instance.TriggerDialogue(1, dialogueView);
                gameObject.SetActive(false);
            }

            if (Input.GetKeyUp(KeyCode.Alpha2))
            {
                DialogueManager.Instance.TriggerDialogue(5, dialogueView);
                gameObject.SetActive(false);
            }
        }
    }
}
