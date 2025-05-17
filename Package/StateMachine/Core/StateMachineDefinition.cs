using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.StateMachine
{
    [CreateAssetMenu(fileName = "NewStateMachine", menuName = "StateMachine/Definition")]
    public class StateMachineDefinition : ScriptableObject
    {
        public string stateMachineName;
        public StateDefinition defaultState;
        public List<StateDefinition> states = new List<StateDefinition>();
        public StateDefinition anyState;

        [System.NonSerialized]
        public Dictionary<string, StateDefinition> statesByID = new Dictionary<string, StateDefinition>();

        public void OnValidate()
        {
            statesByID.Clear();
            foreach (var state in states)
            {
                if (state != null && !string.IsNullOrEmpty(state.stateID))
                {
                    statesByID[state.stateID] = state;
                }
            }
        }
    }
}
