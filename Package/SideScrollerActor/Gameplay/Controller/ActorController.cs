using KahaGameCore.GameEvent;
using KahaGameCore.Package.SideScrollerActor.Gameplay.Controller.Command;
using KahaGameCore.Package.SideScrollerActor.InGameEvent;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Gameplay.Controller
{
    public class ActorController : MonoBehaviour
    {
        [SerializeField] private Actor controlTarget;
        [SerializeField] private ControllerCommandBase[] controllerCommands;

        private void OnEnable()
        {
            if (controlTarget != null)
            {
                SetControlTarget(controlTarget);
            }

            EventBus.Subscribe<Game_OnNextLevelRequested>(OnNextLevelRequested);
        }

        public void SetControlTarget(Actor actor)
        {
            controlTarget = actor;
            for (int i = 0; i < controllerCommands.Length; i++)
            {
                controllerCommands[i].Bind(controlTarget);
            }
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<Game_OnNextLevelRequested>(OnNextLevelRequested);
        }

        private void OnNextLevelRequested(Game_OnNextLevelRequested e)
        {
            gameObject.SetActive(false);
        }
    }
}
