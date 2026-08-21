using System;
using System.Collections.Generic;

namespace KahaGameCore.Effects
{
    public sealed class EffectCommandServiceRegistry
    {
        private readonly Dictionary<Type, object> services =
            new Dictionary<Type, object>();

        public EffectCommandServiceRegistry Add<T>(T service) where T : class
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
