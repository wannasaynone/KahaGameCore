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
    }
}
