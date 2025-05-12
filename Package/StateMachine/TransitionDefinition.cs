using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.StateMachine
{
    [CreateAssetMenu(fileName = "NewTransition", menuName = "StateMachine/Transition")]
    public class TransitionDefinition : ScriptableObject
    {
        public string transitionID;
        public string targetStateID;

        public List<ConditionDefinition> conditions = new List<ConditionDefinition>();

        public Color lineColor = Color.white;
    }
}
