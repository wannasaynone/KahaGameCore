using KahaGameCore.Package.SideScrollerActor.InGameEvent;
using TMPro;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.View
{
    public class TitleView : MonoBehaviour
    {
        public string StartGameSequence => startGameSequence;
        [SerializeField] private TextMeshProUGUI versionText;
        [SerializeField] private string startGameSequence = "GameStartSequence";

        private void Awake()
        {
            // Set the version text to the current version of the game
            versionText.text = Application.version;
        }

        public void Button_StartGame()
        {
            KahaGameCore.GameEvent.EventBus.Publish(new TitleView_OnStartGameRequested
            {
                sequenceName = startGameSequence
            });
        }
    }
}