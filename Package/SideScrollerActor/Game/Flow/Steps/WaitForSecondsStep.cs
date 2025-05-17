using System.Collections;
using KahaGameCore.Common;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Game.Flow.Steps
{
    [CreateAssetMenu(fileName = "WaitForSecondsStep", menuName = "Game Flow/Steps/Wait For Seconds")]
    public class WaitForSecondsStep : GameFlowStep
    {
        public float waitTime = 1.0f;

        private FlowContext currentContext;

        public override void Execute(FlowContext context)
        {
            currentContext = context;

            // Use the GeneralCoroutineRunner to start the wait coroutine
            GeneralCoroutineRunner.Instance.StartCoroutine(IEWait());
        }

        private IEnumerator IEWait()
        {
            yield return new WaitForSeconds(waitTime);
            CompleteStep(currentContext);
        }
    }
}
