using KahaGameCore.Package.SideScrollerActor.Level;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Game.Flow.Steps
{
    [CreateAssetMenu(fileName = "ActivateGameObjectStep", menuName = "Game Flow/Steps/Activate GameObject")]
    public class ActivateGameObjectStep : GameFlowStep
    {
        public enum TargetType { InGameView, Custom, TitleView }

        public TargetType targetType;
        public string gameObjectName;
        public bool activate = true;

        public override void Execute(FlowContext context)
        {
            if (targetType == TargetType.InGameView && context.InGameView != null)
            {
                context.InGameView.gameObject.SetActive(activate);

                if (activate)
                {
                    context.InGameView.UpdateDelayImmediatly();
                }

                CompleteStep(context);
            }
            else if (targetType == TargetType.Custom && !string.IsNullOrEmpty(gameObjectName))
            {
                // Find the game object by path and activate/deactivate it
                GameObject targetObject = LevelManager.GetSpecialGameObjectByName(gameObjectName);

                if (targetObject != null)
                {
                    targetObject.SetActive(activate);
                    CompleteStep(context);
                }
                else
                {
                    Debug.LogError($"Could not find game object at path: {gameObjectName}");
                    CompleteStep(context);
                }
            }
            else if (targetType == TargetType.TitleView && context.TitleView != null)
            {
                context.TitleView.gameObject.SetActive(activate);

                CompleteStep(context);
            }
            else
            {
                Debug.LogError("Invalid target configuration for ActivateGameObjectStep");
                CompleteStep(context);
            }
        }
    }
}
