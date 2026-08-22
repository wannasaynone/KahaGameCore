using System;
using System.Collections.Generic;
using KahaGameCore.Expressions;
using KahaGameCore.Parameters;
using UnityEngine;

namespace KahaGameCore.Presentation
{
    [Serializable]
    public sealed class ParameterChildConditionBinding
    {
        [SerializeField]
        private GameObject target;

        [SerializeField]
        private string condition;

        public ParameterChildConditionBinding(GameObject target, string condition)
        {
            this.target = target;
            this.condition = condition;
        }

        public GameObject Target => target;
        public string Condition => condition;
    }

    [DisallowMultipleComponent]
    public sealed class ParameterStateBinder : MonoBehaviour
    {
        [SerializeField]
        private List<ParameterChildConditionBinding> bindings =
            new List<ParameterChildConditionBinding>();

        [SerializeField]
        private List<Behaviour> behaviourTargets = new List<Behaviour>();

        [SerializeField]
        private string behaviourCondition;

        [SerializeField, HideInInspector]
        private bool behaviourTargetsManaged;

        private ParameterStore parameters;

        public IReadOnlyList<Behaviour> BehaviourTargets => behaviourTargets;
        public string BehaviourCondition => behaviourCondition;
        public bool BehaviourTargetsManaged => behaviourTargetsManaged;

        public void Configure(
            IEnumerable<ParameterChildConditionBinding> conditionBindings)
        {
            if (parameters != null)
                throw new InvalidOperationException(
                    "ParameterStateBinder cannot be configured after initialization.");

            List<ParameterChildConditionBinding> configuredBindings =
                new List<ParameterChildConditionBinding>(conditionBindings ??
                    throw new ArgumentNullException(nameof(conditionBindings)));
            ValidateBindings(configuredBindings);
            bindings = configuredBindings;
        }

        public void ConfigureBehaviourBinding(
            IEnumerable<Behaviour> targets,
            string condition)
        {
            ConfigureBehaviourBinding(targets, condition, false);
        }

        public void ConfigureManagedBehaviourBinding(
            IEnumerable<Behaviour> targets,
            string condition)
        {
            ConfigureBehaviourBinding(targets, condition, true);
        }

        private void ConfigureBehaviourBinding(
            IEnumerable<Behaviour> targets,
            string condition,
            bool targetsManaged)
        {
            if (parameters != null)
                throw new InvalidOperationException(
                    "ParameterStateBinder cannot be configured after initialization.");

            List<Behaviour> configuredTargets = new List<Behaviour>(targets ??
                throw new ArgumentNullException(nameof(targets)));
            ValidateBehaviourTargets(configuredTargets);
            behaviourTargets = configuredTargets;
            behaviourCondition = condition ?? string.Empty;
            behaviourTargetsManaged = targetsManaged;
        }

        public void Initialize(ParameterStore parameterStore)
        {
            if (parameterStore == null)
                throw new ArgumentNullException(nameof(parameterStore));

            Unsubscribe();
            parameters = null;

            ValidateConfiguration();
            bool[] childActiveStates = EvaluateBindings(parameterStore);
            bool behaviourIsEnabled = EvaluateBehaviourCondition(parameterStore);

            Apply(childActiveStates, behaviourIsEnabled);
            parameters = parameterStore;
            parameters.Changed += OnParameterChanged;
        }

        public void Refresh()
        {
            if (parameters == null)
                throw new InvalidOperationException(
                    "ParameterStateBinder is not initialized.");

            ValidateConfiguration();
            bool[] childActiveStates = EvaluateBindings(parameters);
            bool behaviourIsEnabled = EvaluateBehaviourCondition(parameters);
            Apply(childActiveStates, behaviourIsEnabled);
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void OnParameterChanged(ParameterChanged change)
        {
            Refresh();
        }

        private bool[] EvaluateBindings(ParameterStore parameterStore)
        {
            bool[] activeStates = new bool[bindings.Count];
            for (int index = 0; index < bindings.Count; index++)
            {
                ParameterChildConditionBinding binding = bindings[index];
                ExpressionResult<bool> result =
                    parameterStore.EvaluateCondition(binding.Condition);
                if (!result.IsSuccess)
                {
                    throw new InvalidOperationException(
                        $"ParameterStateBinder condition for '{binding.Target.name}' failed: " +
                        result.Error);
                }

                activeStates[index] = result.Value;
            }

            return activeStates;
        }

        private bool EvaluateBehaviourCondition(ParameterStore parameterStore)
        {
            if (behaviourTargets.Count == 0)
            {
                return false;
            }

            ExpressionResult<bool> result =
                parameterStore.EvaluateCondition(behaviourCondition);
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(
                    "ParameterStateBinder behaviour condition failed: " +
                    result.Error);
            }

            return result.Value;
        }

        private void Apply(bool[] childActiveStates, bool behaviourIsEnabled)
        {
            for (int index = 0; index < bindings.Count; index++)
            {
                bindings[index].Target.SetActive(childActiveStates[index]);
            }

            for (int index = 0; index < behaviourTargets.Count; index++)
            {
                behaviourTargets[index].enabled = behaviourIsEnabled;
            }
        }

        private void ValidateConfiguration()
        {
            if (bindings.Count == 0 && behaviourTargets.Count == 0)
            {
                throw new InvalidOperationException(
                    "ParameterStateBinder requires at least one binding.");
            }

            ValidateBindings(bindings);
            ValidateBehaviourTargets(behaviourTargets);
            if (behaviourTargets.Count > 0 &&
                string.IsNullOrWhiteSpace(behaviourCondition))
            {
                throw new InvalidOperationException(
                    "ParameterStateBinder behaviour binding requires a condition.");
            }
        }

        private void ValidateBindings(
            IReadOnlyList<ParameterChildConditionBinding> candidateBindings)
        {
            if (candidateBindings == null)
                throw new ArgumentNullException(nameof(candidateBindings));

            HashSet<GameObject> targets = new HashSet<GameObject>();
            for (int index = 0; index < candidateBindings.Count; index++)
            {
                ParameterChildConditionBinding binding = candidateBindings[index];
                if (binding == null)
                    throw new InvalidOperationException(
                        "Parameter child condition binding cannot be null.");
                if (binding.Target == null)
                    throw new InvalidOperationException(
                        "Parameter child condition binding requires a target.");
                if (binding.Target.transform == transform ||
                    !binding.Target.transform.IsChildOf(transform))
                {
                    throw new InvalidOperationException(
                        $"Parameter state target '{binding.Target.name}' must be a child of " +
                        $"'{name}'.");
                }
                if (string.IsNullOrWhiteSpace(binding.Condition))
                    throw new InvalidOperationException(
                        $"Parameter state target '{binding.Target.name}' requires a condition.");
                if (!targets.Add(binding.Target))
                    throw new InvalidOperationException(
                        $"Parameter state target '{binding.Target.name}' is bound more than once.");
            }
        }

        private void ValidateBehaviourTargets(
            IReadOnlyList<Behaviour> candidateTargets)
        {
            if (candidateTargets == null)
                throw new ArgumentNullException(nameof(candidateTargets));

            HashSet<Behaviour> targets = new HashSet<Behaviour>();
            for (int index = 0; index < candidateTargets.Count; index++)
            {
                Behaviour target = candidateTargets[index];
                if (target == null)
                    throw new InvalidOperationException(
                        "Parameter behaviour target cannot be null.");
                if (ReferenceEquals(target, this))
                    throw new InvalidOperationException(
                        "ParameterStateBinder cannot control itself.");
                if (target.transform != transform &&
                    !target.transform.IsChildOf(transform))
                {
                    throw new InvalidOperationException(
                        $"Parameter behaviour target '{target.name}' must be on " +
                        $"'{name}' or one of its children.");
                }
                if (!targets.Add(target))
                    throw new InvalidOperationException(
                        $"Parameter behaviour target '{target.name}' is bound more than once.");
            }
        }

        private void Unsubscribe()
        {
            if (parameters != null)
            {
                parameters.Changed -= OnParameterChanged;
            }
        }
    }
}
