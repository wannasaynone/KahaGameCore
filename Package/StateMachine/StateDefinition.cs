using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.StateMachine
{
    [CreateAssetMenu(fileName = "NewState", menuName = "StateMachine/State")]
    public class StateDefinition : ScriptableObject
    {
        public string stateID;
        public string stateName;

        public Vector2 editorPosition;

        public List<TransitionDefinition> transitions = new List<TransitionDefinition>();

        public static float stateTimer = 0f;

        public StateBehaviourDefinition enterBehaviour;
        public StateBehaviourDefinition exitBehaviour;
        public StateBehaviourDefinition updateBehaviour;
    }
}
