using System;
using System.Collections.Generic;
using KahaGameCore.Effects;

namespace KahaGameCore.GameEvents.Editor
{
    /// <summary>
    /// Name → descriptor for every command an event can contain: the registered
    /// commands plus the built-in control flow (If／Else／EndIf／Quit). The
    /// built-ins win a name clash, because GameEventRunner interprets them
    /// before any registered command is looked up.
    /// </summary>
    internal static class GameEventCommandDescriptorLookup
    {
        public static Dictionary<string, EffectCommandDescriptor> Create(
            IEnumerable<EffectCommandDescriptor> registeredDescriptors)
        {
            Dictionary<string, EffectCommandDescriptor> commandsByName =
                new Dictionary<string, EffectCommandDescriptor>(StringComparer.Ordinal);
            foreach (EffectCommandDescriptor descriptor in
                     registeredDescriptors ?? Array.Empty<EffectCommandDescriptor>())
            {
                commandsByName[descriptor.Name] = descriptor;
            }

            foreach (EffectCommandDescriptor descriptor in GameEventControlFlow.Descriptors)
            {
                commandsByName[descriptor.Name] = descriptor;
            }

            return commandsByName;
        }
    }
}
