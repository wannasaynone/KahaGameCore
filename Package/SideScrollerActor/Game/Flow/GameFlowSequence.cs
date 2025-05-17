using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Game.Flow
{
    [CreateAssetMenu(fileName = "_Sequence", menuName = "Game Flow/Sequence")]
    public class GameFlowSequence : ScriptableObject
    {
        public event System.Action OnSequenceCompleted;

        [TextArea]
        public string description;

        public List<GameFlowStep> steps = new List<GameFlowStep>();

        private int currentStepIndex = -1;
        private FlowContext context;
        private GameFlowStep currentStep;

        public void ExecuteSequence(FlowContext initialContext)
        {
            context = initialContext;
            currentStepIndex = -1;
            ExecuteNextStep();
        }

        public void ExecuteNextStep()
        {
            currentStepIndex++;

            if (currentStepIndex >= steps.Count)
            {
                // Sequence completed
                Debug.Log($"Game flow sequence '{name}' completed.");
                OnSequenceCompleted?.Invoke();
                return;
            }

            currentStep = steps[currentStepIndex];
            Debug.Log($"Executing step {currentStepIndex + 1}/{steps.Count}: {currentStep.name}");

            // Subscribe to step completion
            currentStep.OnStepCompleted += OnCurrentStepComplete;

            // Execute the step
            currentStep.Execute(context);
        }

        private void OnCurrentStepComplete(FlowContext updatedContext)
        {
            currentStep.OnStepCompleted -= OnCurrentStepComplete;
            // Update context with any changes from the step
            context = updatedContext;

            // Move to the next step
            ExecuteNextStep();
        }
    }
}
