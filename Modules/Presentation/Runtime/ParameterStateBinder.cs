using System;
using System.Collections.Generic;
using KahaGameCore.Expressions;
using KahaGameCore.Parameters;
using UnityEngine;
using ExpressionEngine = KahaGameCore.Expressions.Expressions;

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

        private ParameterStore parameters;
        private ExpressionEngine expressionEngine;
        private IExpressionContext expressionContext;

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

        public void Initialize(ParameterStore parameterStore)
        {
            if (parameterStore == null)
                throw new ArgumentNullException(nameof(parameterStore));

            Unsubscribe();
            parameters = null;
            expressionEngine = null;
            expressionContext = null;

            ValidateBindings(bindings);
            ExpressionEngine nextExpressionEngine = new ExpressionEngine();
            IExpressionContext nextExpressionContext =
                new ParameterExpressionContext(parameterStore);
            bool[] activeStates = EvaluateBindings(
                nextExpressionEngine,
                nextExpressionContext);

            Apply(activeStates);
            parameters = parameterStore;
            expressionEngine = nextExpressionEngine;
            expressionContext = nextExpressionContext;
            parameters.Changed += OnParameterChanged;
        }

        public void Refresh()
        {
            if (parameters == null || expressionEngine == null || expressionContext == null)
                throw new InvalidOperationException(
                    "ParameterStateBinder is not initialized.");

            ValidateBindings(bindings);
            bool[] activeStates = EvaluateBindings(expressionEngine, expressionContext);
            Apply(activeStates);
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void OnParameterChanged(ParameterChanged change)
        {
            Refresh();
        }

        private bool[] EvaluateBindings(
            ExpressionEngine evaluator,
            IExpressionContext context)
        {
            bool[] activeStates = new bool[bindings.Count];
            for (int index = 0; index < bindings.Count; index++)
            {
                ParameterChildConditionBinding binding = bindings[index];
                ExpressionResult<bool> result = evaluator.EvaluateCondition(
                    binding.Condition,
                    context);
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

        private void Apply(bool[] activeStates)
        {
            for (int index = 0; index < bindings.Count; index++)
            {
                bindings[index].Target.SetActive(activeStates[index]);
            }
        }

        private void ValidateBindings(
            IReadOnlyList<ParameterChildConditionBinding> candidateBindings)
        {
            if (candidateBindings == null || candidateBindings.Count == 0)
                throw new InvalidOperationException(
                    "ParameterStateBinder requires at least one child binding.");

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

        private void Unsubscribe()
        {
            if (parameters != null)
            {
                parameters.Changed -= OnParameterChanged;
            }
        }
    }
}
