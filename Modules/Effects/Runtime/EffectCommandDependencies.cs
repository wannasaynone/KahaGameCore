using System;
using System.Collections.Generic;

namespace KahaGameCore.Effects
{
    /// <summary>
    /// The ingredients a command factory may ask for, keyed by type. Filled by the
    /// composition root and read only while <see cref="EffectCommandBootstrapper"/> is
    /// building commands; nothing touches it once startup is done. The finished commands
    /// live in <see cref="EffectCommandRegistry"/> instead.
    /// </summary>
    public sealed class EffectCommandDependencies
    {
        private readonly Dictionary<Type, object> services =
            new Dictionary<Type, object>();

        /// <summary>
        /// Registers a service under <typeparamref name="T"/>. The key is the declared
        /// type, not the runtime type, so Add&lt;object&gt;(store) is not found by
        /// GetRequired&lt;ParameterStore&gt;().
        /// </summary>
        public EffectCommandDependencies Add<T>(T service) where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            Type serviceType = typeof(T);
            if (services.ContainsKey(serviceType))
            {
                throw new EffectCommandCompositionException(
                    $"Effect command service '{serviceType.FullName}' was added more than once.");
            }

            services.Add(serviceType, service);
            return this;
        }

        public T GetRequired<T>() where T : class
        {
            if (services.TryGetValue(typeof(T), out object service))
                return (T)service;

            throw new EffectCommandCompositionException(
                $"Required effect command service '{typeof(T).FullName}' is missing.");
        }
    }
}
