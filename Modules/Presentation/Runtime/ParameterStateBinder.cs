using System;
using System.Collections.Generic;
using KahaGameCore.Parameters;
using UnityEngine;

namespace KahaGameCore.Presentation
{
    [Serializable]
    public sealed class ParameterChildStateMapping
    {
        [SerializeField]
        private int value;

        [SerializeField]
        private int childIndex;

        public ParameterChildStateMapping(int value, int childIndex)
        {
            this.value = value;
            this.childIndex = childIndex;
        }

        public int Value => value;
        public int ChildIndex => childIndex;
    }

    [DisallowMultipleComponent]
    public sealed class ParameterStateBinder : MonoBehaviour
    {
        [SerializeField]
        private string parameterKey;

        [SerializeField]
        private Transform stateRoot;

        [SerializeField]
        private List<ParameterChildStateMapping> mappings =
            new List<ParameterChildStateMapping>();

        private ParameterStore parameters;

        public void Configure(
            string key,
            Transform root,
            IEnumerable<ParameterChildStateMapping> stateMappings)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Parameter key is required.", nameof(key));
            parameterKey = key;
            stateRoot = root ?? throw new ArgumentNullException(nameof(root));
            mappings = new List<ParameterChildStateMapping>(
                stateMappings ?? throw new ArgumentNullException(nameof(stateMappings)));
            ValidateMappings();
        }

        public void Initialize(ParameterStore parameterStore)
        {
            if (parameters != null)
            {
                parameters.Changed -= OnParameterChanged;
            }

            parameters = parameterStore ?? throw new ArgumentNullException(nameof(parameterStore));
            ValidateConfiguration();
            parameters.Changed += OnParameterChanged;
            Refresh();
        }

        public void Refresh()
        {
            ValidateConfiguration();
            if (!parameters.TryGetValue(parameterKey, out ParameterValue value))
            {
                throw new UnknownParameterException(parameterKey);
            }

            if (value.Type != ParameterType.Int)
            {
                throw new ParameterTypeMismatchException(
                    parameterKey,
                    value.Type,
                    ParameterType.Int);
            }

            int activeChildIndex = -1;
            int currentValue = value.AsInt();
            for (int index = 0; index < mappings.Count; index++)
            {
                if (mappings[index].Value == currentValue)
                {
                    activeChildIndex = mappings[index].ChildIndex;
                    break;
                }
            }

            for (int index = 0; index < stateRoot.childCount; index++)
            {
                stateRoot.GetChild(index).gameObject.SetActive(index == activeChildIndex);
            }
        }

        private void OnDestroy()
        {
            if (parameters != null)
            {
                parameters.Changed -= OnParameterChanged;
            }
        }

        private void OnParameterChanged(ParameterChanged change)
        {
            if (string.Equals(change.Key, parameterKey, StringComparison.Ordinal))
            {
                Refresh();
            }
        }

        private void ValidateConfiguration()
        {
            if (parameters == null)
                throw new InvalidOperationException("ParameterStateBinder is not initialized.");
            if (string.IsNullOrWhiteSpace(parameterKey))
                throw new InvalidOperationException("ParameterStateBinder requires a parameter key.");
            if (stateRoot == null)
                throw new InvalidOperationException("ParameterStateBinder requires a state root.");
            ValidateMappings();
        }

        private void ValidateMappings()
        {
            HashSet<int> values = new HashSet<int>();
            for (int index = 0; index < mappings.Count; index++)
            {
                ParameterChildStateMapping mapping = mappings[index];
                if (mapping == null)
                    throw new InvalidOperationException("Parameter state mapping cannot be null.");
                if (!values.Add(mapping.Value))
                    throw new InvalidOperationException(
                        $"Parameter state value '{mapping.Value}' is mapped more than once.");
                if (stateRoot != null &&
                    (mapping.ChildIndex < 0 || mapping.ChildIndex >= stateRoot.childCount))
                {
                    throw new InvalidOperationException(
                        $"Parameter state child index '{mapping.ChildIndex}' is out of range.");
                }
            }
        }
    }
}
