using System;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Game.Flow
{
    public abstract class GameFlowStep : ScriptableObject
    {
        [TextArea]
        public string description;

        // Event for step completion
        public event Action<FlowContext> OnStepCompleted;

        // Execute the step
        public abstract void Execute(FlowContext context);

        // Called when the step is completed
        protected void CompleteStep(FlowContext context)
        {
            OnStepCompleted?.Invoke(context);
        }
    }
}
