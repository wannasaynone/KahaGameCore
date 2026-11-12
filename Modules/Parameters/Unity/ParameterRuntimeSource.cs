using System;
using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.Parameters
{
    [DisallowMultipleComponent]
    public abstract class ParameterRuntimeSource : MonoBehaviour
    {
        private ParameterStore parameterStore;

        public bool IsInitialized => parameterStore != null;

        public void Initialize(ParameterStore store)
        {
            parameterStore = store ?? throw new ArgumentNullException(nameof(store));
        }

        public IReadOnlyList<ParameterRuntimeValue> CaptureCurrentValues()
        {
            return parameterStore == null
                ? Array.Empty<ParameterRuntimeValue>()
                : parameterStore.CaptureCurrentValues();
        }

        /// <summary>
        /// Writes through ParameterStore.Set, so the value is clamped to the
        /// definition's range and Changed fires like any other write.
        /// </summary>
        public void SetValue(string key, ParameterValue value)
        {
            if (parameterStore == null)
            {
                throw new InvalidOperationException(
                    "ParameterRuntimeSource is not initialized.");
            }

            switch (value.Type)
            {
                case ParameterType.Int:
                    parameterStore.Set(key, value.AsInt());
                    break;
                case ParameterType.Float:
                    parameterStore.Set(key, value.AsFloat());
                    break;
                case ParameterType.Bool:
                    parameterStore.Set(key, value.AsBool());
                    break;
                case ParameterType.String:
                    parameterStore.Set(key, value.AsString());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value));
            }
        }
    }
}
